package com.github.keanucz.logigit.ipc

import kotlinx.serialization.SerialName
import kotlinx.serialization.Serializable
import kotlinx.serialization.json.Json
import kotlinx.serialization.json.JsonElement
import kotlinx.serialization.json.decodeFromJsonElement

object LogiGitMessageParser {
    private val json = Json { ignoreUnknownKeys = true }

    fun parse(raw: String): LogiGitMessage = json.decodeFromString(LogiGitMessage.serializer(), raw)
}

@Serializable
data class LogiGitMessage(
    @SerialName("schemaVersion") val schemaVersion: Int = 1,
    val type: String,
    val correlationId: String = "",
    val payload: Map<String, JsonElement> = emptyMap()
)
