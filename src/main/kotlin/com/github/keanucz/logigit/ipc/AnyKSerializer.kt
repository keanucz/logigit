package com.github.keanucz.logigit.ipc

import kotlinx.serialization.KSerializer
import kotlinx.serialization.SerializationException
import kotlinx.serialization.descriptors.SerialDescriptor
import kotlinx.serialization.descriptors.buildClassSerialDescriptor
import kotlinx.serialization.encoding.Decoder
import kotlinx.serialization.encoding.Encoder
import kotlinx.serialization.json.JsonDecoder
import kotlinx.serialization.json.JsonElement
import kotlinx.serialization.json.JsonEncoder
import kotlinx.serialization.json.JsonPrimitive
import kotlinx.serialization.json.booleanOrNull
import kotlinx.serialization.json.doubleOrNull
import kotlinx.serialization.json.floatOrNull
import kotlinx.serialization.json.intOrNull
import kotlinx.serialization.json.longOrNull
import kotlinx.serialization.json.contentOrNull

object AnyKSerializer : KSerializer<Any?> {
    override val descriptor: SerialDescriptor = buildClassSerialDescriptor("LogiGitAny")

    override fun serialize(encoder: Encoder, value: Any?) {
        if (encoder !is JsonEncoder) throw SerializationException("Only JSON encoding is supported")
        val element = when (value) {
            null -> JsonPrimitive(null as String?)
            is Number -> JsonPrimitive(value)
            is Boolean -> JsonPrimitive(value)
            else -> JsonPrimitive(value.toString())
        }
        encoder.encodeJsonElement(element)
    }

    override fun deserialize(decoder: Decoder): Any? {
        if (decoder !is JsonDecoder) throw SerializationException("Only JSON decoding is supported")
        val element: JsonElement = decoder.decodeJsonElement()
        return when (element) {
            is JsonPrimitive -> element.booleanOrNull ?: element.intOrNull ?: element.longOrNull
                ?: element.floatOrNull ?: element.doubleOrNull ?: element.contentOrNull
            else -> element.toString()
        }
    }
}
