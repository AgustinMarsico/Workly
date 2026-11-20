namespace WorklyReservationService.IDataAccess.External;

public sealed record AreaUserResponse(
    Guid Id,
    string Email,
    string? Name,
    Guid? AreaId,
    bool EmailSubscribed);
