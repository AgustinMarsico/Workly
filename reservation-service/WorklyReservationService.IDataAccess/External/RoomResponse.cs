namespace WorklyReservationService.IDataAccess.External;

public sealed record RoomResponse(
    Guid Id,
    Guid CompanyId,
    string Name,
    string TypeKey,
    string TypeDisplayName,
    int MaxCapacity,
    string? Equipment);
