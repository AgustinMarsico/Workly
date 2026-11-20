using System.Text.Json.Serialization;

namespace IBusinessLogic.Rooms;

public record RoomDto(
    Guid Id,
    [property: JsonPropertyName("companyId")] Guid CompanyId,
    string Name,
    string TypeKey,
    string TypeDisplayName,
    int MaxCapacity,
    string? Equipment);
