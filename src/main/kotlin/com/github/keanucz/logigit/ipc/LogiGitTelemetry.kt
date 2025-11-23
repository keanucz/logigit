package com.github.keanucz.logigit.ipc

import com.intellij.openapi.Disposable
import com.intellij.openapi.application.ApplicationManager
import com.intellij.openapi.diagnostic.Logger
import com.intellij.util.concurrency.AppExecutorUtil
import kotlinx.serialization.SerialName
import kotlinx.serialization.Serializable
import kotlinx.serialization.encodeToString
import kotlinx.serialization.json.Json
import java.net.URI
import java.net.http.HttpClient
import java.net.http.HttpRequest
import java.net.http.HttpResponse
import java.time.Instant
import java.util.UUID
import java.util.concurrent.LinkedBlockingQueue
import java.util.concurrent.TimeUnit

class LogiGitTelemetry(
    private val baseUrl: String = resolveBaseUrl(),
    private val client: HttpClient = defaultHttpClient()
) : Disposable {
    private val logger = Logger.getInstance(LogiGitTelemetry::class.java)
    private val httpClient = client
    private val json = Json { encodeDefaults = true }
    private val queue = LinkedBlockingQueue<TelemetryEvent>()
    private val worker = AppExecutorUtil.createBoundedApplicationPoolExecutor("LogiGitTelemetry", 1)
    @Volatile
    private var disposed = false

    init {
        worker.execute { processQueue() }
    }

    fun event(name: String, correlationId: String = UUID.randomUUID().toString(), payload: Map<String, Any?> = emptyMap()) {
        if (disposed) {
            logger.warn("Telemetry already disposed; dropping $name")
            return
        }

        queue.offer(TelemetryEvent(name, correlationId, payload))
    }

    override fun dispose() {
        disposed = true
        worker.shutdownNow()
    }

    private fun processQueue() {
        while (!disposed) {
            try {
                val event = queue.poll(500, TimeUnit.MILLISECONDS) ?: continue
                send(event)
            } catch (_: InterruptedException) {
                Thread.currentThread().interrupt()
                return
            } catch (t: Throwable) {
                logger.warn("Failed to send telemetry", t)
            }
        }
    }

    internal fun send(event: TelemetryEvent) {
        val envelope = TelemetryEnvelope(
            correlationId = event.correlationId,
            emittedAt = Instant.now().toString(),
            payload = event.payload
        )

        val body = json.encodeToString(envelope)
        val target = URI.create("$baseUrl/api/events/${event.name}")
        val request = HttpRequest.newBuilder(target)
            .header("Content-Type", "application/json")
            .POST(HttpRequest.BodyPublishers.ofString(body))
            .build()

        httpClient.sendAsync(request, HttpResponse.BodyHandlers.discarding())
            .orTimeout(5, TimeUnit.SECONDS)
            .exceptionally {
                logger.warn("Telemetry send failed for ${event.name}: ${it.message}")
                null
            }
    }

    internal data class TelemetryEvent(
        val name: String,
        val correlationId: String,
        val payload: Map<String, Any?>
    )

    @Serializable
    private data class TelemetryEnvelope(
        @SerialName("correlationId") val correlationId: String,
        @SerialName("emittedAt") val emittedAt: String,
        @SerialName("payload") val payload: Map<String, @Serializable(with = AnyKSerializer::class) Any?>
    )

    companion object {
        fun instance(): LogiGitTelemetry = ApplicationManager.getApplication().getService(LogiGitTelemetry::class.java)

        private fun defaultHttpClient(): HttpClient = HttpClient.newBuilder()
            .connectTimeout(java.time.Duration.ofSeconds(2))
            .build()

        private fun resolveBaseUrl(): String {
            val fromProp = System.getProperty("idea.logigit.telemetry.url")
            val fromEnv = System.getenv("LOGIGIT_IDE_TELEMETRY_URL")
            return fromProp?.takeIf { it.isNotBlank() }
                ?: fromEnv?.takeIf { it.isNotBlank() }
                ?: "http://localhost:5056"
        }
    }
}
