package com.github.keanucz.logigit.git

import com.github.keanucz.logigit.ipc.LogiGitMessage
import com.github.keanucz.logigit.ipc.LogiGitTelemetry
import kotlinx.serialization.json.Json
import kotlinx.serialization.json.buildJsonObject
import kotlinx.serialization.json.put
import org.junit.Test
import java.util.concurrent.atomic.AtomicInteger

class LogiGitCommandExecutorTest {
    private val telemetryEvents = mutableListOf<String>()

    private val telemetry = object : LogiGitTelemetry("http://localhost") {
        override fun event(name: String, correlationId: String, payload: Map<String, Any?>) {
            telemetryEvents.add("$name:$payload")
        }
    }

    @Test
    fun `denies git intent if repo dirty`() {
        val executor = object : LogiGitCommandExecutor(telemetry = telemetry, gitRunner = GitRunner { _, _, _ -> GitCliResult(0, "", "") }) {
            override fun resolveGitRoot(project: com.intellij.openapi.project.Project) = object : com.intellij.openapi.vfs.VirtualFile() {
                override fun getPath(): String = "."
                override fun getName(): String = "root"
                override fun getFileSystem() = throw UnsupportedOperationException()
                override fun getParent() = null
                override fun getChildren() = emptyArray<com.intellij.openapi.vfs.VirtualFile>()
                override fun isWritable() = true
                override fun isDirectory() = true
                override fun isValid() = true
                override fun getModificationStamp() = 0L
                override fun getTimeStamp() = 0L
                override fun getLength() = 0L
                override fun refresh(asynchronous: Boolean, recursive: Boolean, postRunnable: Runnable?) {}
                override fun getInputStream() = throw UnsupportedOperationException()
                override fun getOutputStream(requestor: Any?, newModificationStamp: Long, newTimeStamp: Long) = throw UnsupportedOperationException()
                override fun contentsToByteArray() = ByteArray(0)
                override fun getCanonicalPath() = path
            }

            override fun isRepoClean(project: com.intellij.openapi.project.Project) = false
        }

        val msg = LogiGitMessage(type = "git.intent", correlationId = "test", payload = buildJsonObject {
            put("command", "git.push")
            put("requiresCleanRepo", true)
        }.toMap())

        executor.handleIntent(msg)
        assert(telemetryEvents.any { it.contains("git.intent.denied") })
    }
}

