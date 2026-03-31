using System.Text.Json;
using Telemetry.Api.Models;

namespace Telemetry.Api.Services;

public static class TelemetryEnvelopeParser
{
    public static StreamedSensorReading? TryParseOutboxEnvelope(string kafkaMessageValue)
    {
        try
        {
            using var doc = JsonDocument.Parse(kafkaMessageValue);
            var root = doc.RootElement;

            long? outboxId = root.TryGetProperty("outboxId", out var ob) && ob.ValueKind == JsonValueKind.Number
                ? ob.GetInt64()
                : null;

            var eventType = root.TryGetProperty("eventType", out var et) ? et.GetString() : null;
            if (string.IsNullOrEmpty(eventType) || !root.TryGetProperty("payload", out var payload))
                return null;

            if (!payload.TryGetProperty("name", out var nameEl))
                return null;

            var deviceId = nameEl.GetString();
            if (string.IsNullOrWhiteSpace(deviceId))
                return null;

            Guid? recordId = payload.TryGetProperty("id", out var idEl) && idEl.ValueKind == JsonValueKind.String
                && Guid.TryParse(idEl.GetString(), out var rid)
                ? rid
                : null;

            DateTimeOffset? createdAt = null;
            if (payload.TryGetProperty("createdAtUtc", out var cAt) && cAt.ValueKind == JsonValueKind.String)
            {
                if (DateTimeOffset.TryParse(cAt.GetString(), out var dto))
                    createdAt = dto;
            }

            string? metric = null;
            double? value = null;
            string? unit = null;
            DateTimeOffset? recordedAt = createdAt;

            if (payload.TryGetProperty("value", out var valueEl) && valueEl.ValueKind == JsonValueKind.String)
            {
                var inner = valueEl.GetString();
                if (!string.IsNullOrWhiteSpace(inner))
                {
                    TryParseSensorJson(inner, ref metric, ref value, ref unit, ref recordedAt);
                }
            }

            return new StreamedSensorReading
            {
                DeviceId = deviceId,
                Metric = metric,
                Value = value,
                Unit = unit,
                RecordedAt = recordedAt,
                EventType = eventType!,
                OutboxId = outboxId,
                RecordId = recordId,
                RawPayloadJson = payload.GetRawText()
            };
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static void TryParseSensorJson(
        string json,
        ref string? metric,
        ref double? value,
        ref string? unit,
        ref DateTimeOffset? recordedAt)
    {
        try
        {
            using var inner = JsonDocument.Parse(json);
            var p = inner.RootElement;
            if (p.TryGetProperty("metric", out var m))
                metric = m.GetString();
            if (p.TryGetProperty("value", out var v) && v.ValueKind == JsonValueKind.Number)
                value = v.GetDouble();
            if (p.TryGetProperty("unit", out var u))
                unit = u.GetString();
            if (p.TryGetProperty("recordedAt", out var r) && r.ValueKind == JsonValueKind.String
                && DateTimeOffset.TryParse(r.GetString(), out var rt))
                recordedAt = rt;
        }
        catch (JsonException)
        {
            // leave sensor fields null; device id still useful
        }
    }
}
