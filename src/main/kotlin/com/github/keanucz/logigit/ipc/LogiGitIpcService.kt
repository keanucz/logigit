package com.github.keanucz.logigit.ipc

import com.github.keanucz.logigit.git.LogiGitCommandExecutor
import com.intellij.openapi.application.ApplicationManager
import com.intellij.openapi.diagnostic.Logger
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.Job
import kotlinx.coroutines.delay
import kotlinx.coroutines.launch
import java.io.BufferedReader
import java.io.InputStreamReader
import java.net.ServerSocket
import java.net.Socket
import java.nio.charset.StandardCharsets
import java.util.UUID
import java.util.concurrent.atomic.AtomicBoolean

class LogiGitIpcService {
    private val logger = Logger.getInstance(LogiGitIpcService::class.java)
    private val telemetry = LogiGitTelemetry.instance()
    private val executor = ApplicationManager.getApplication().getService(LogiGitCommandExecutor::class.java)
    private val scope = CoroutineScope(Dispatchers.IO)
    private val running = AtomicBoolean(false)
    private var job: Job? = null

    fun start() {
        if (running.getAndSet(true)) return
        job = scope.launch { listenLoop() }
    }

    fun stop() {
        running.set(false)
        job?.cancel()
    }

    private suspend fun listenLoop() {
        var attempt = 0
        while (running.get()) {
            try {
                ServerSocket(58555).use { server ->
                    logger.info("LogiGit IPC listening on 58555")
                    telemetry.event("ipc.listening", payload = mapOf("port" to 58555))
                    val socket = server.accept()
                    handleClient(socket)
                }
            } catch (t: Throwable) {
                attempt++
                logger.warn("IPC server error", t)
                telemetry.event("ipc.error", payload = mapOf("attempt" to attempt, "message" to t.message))
                delay((attempt * 1000L).coerceAtMost(10_000L))
            }
        }
    }

    private fun handleClient(socket: Socket) {
        scope.launch {
            val correlationId = UUID.randomUUID().toString()
            telemetry.event("ipc.client.connected", correlationId, mapOf("remote" to socket.inetAddress.hostAddress))
            socket.use { s ->
                val reader = BufferedReader(InputStreamReader(s.getInputStream(), StandardCharsets.UTF_8))
                while (running.get()) {
                    val line = reader.readLine() ?: break
                    handleMessage(line)
                }
            }
            telemetry.event("ipc.client.disconnected", correlationId)
        }
    }

    private fun handleMessage(raw: String) {
        try {
            val message = LogiGitMessageParser.parse(raw)
            telemetry.event("ipc.message", message.correlationId.ifEmpty { UUID.randomUUID().toString() }, mapOf("type" to message.type))
            when (message.type) {
                "git.intent" -> executor.handleIntent(message)
                "dial.scroll" -> executor.handleScroll(message)
                "gesture" -> executor.handleGesture(message)
                else -> logger.warn("Unknown message type: ${message.type}")
            }
        } catch (t: Throwable) {
            logger.warn("Failed to handle IPC payload: $raw", t)
            telemetry.event("ipc.message.error", payload = mapOf("error" to t.message, "payload" to raw))
        }
    }

    companion object {
        fun instance(): LogiGitIpcService = ApplicationManager.getApplication().getService(LogiGitIpcService::class.java)
    }
}
