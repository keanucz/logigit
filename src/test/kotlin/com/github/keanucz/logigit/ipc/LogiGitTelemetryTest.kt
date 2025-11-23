package com.github.keanucz.logigit.ipc

import com.intellij.testFramework.LightPlatformTestCase
import okhttp3.mockwebserver.MockResponse
import okhttp3.mockwebserver.MockWebServer
import org.junit.Test
import java.net.http.HttpClient

class LogiGitTelemetryTest : LightPlatformTestCase() {
    @Test
    fun `telemetry enqueues and sends`() {
        val server = MockWebServer()
        server.enqueue(MockResponse().setResponseCode(200))
        server.start()

        val telemetry = LogiGitTelemetry(baseUrl = server.url("/").toString().trimEnd('/'), client = HttpClient.newHttpClient())
        telemetry.event("git.intent", payload = mapOf("command" to "git.status"))

        Thread.sleep(500)
        telemetry.dispose()

        val request = server.takeRequest()
        assertEquals("POST", request.method)
        assertTrue(request.path!!.contains("/api/events/git.intent"))
        assertTrue(request.body.readUtf8().contains("git.status"))

        server.shutdown()
    }
}

