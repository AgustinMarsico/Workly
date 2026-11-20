using System.Text.Json;

namespace AuditService.Api.Domain;

public static class AuditJson
{
    public static string? ToRawTextOrNull(JsonElement? value)
    {
        if (!value.HasValue || value.Value.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null)
        {
            return null;
        }

        return value.Value.GetRawText();
    }
}
