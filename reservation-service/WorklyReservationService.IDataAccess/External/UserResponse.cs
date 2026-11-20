namespace WorklyReservationService.IDataAccess.External;

public sealed record UserResponse(
    Guid Id,
    string Email,
    string? Name,
    string Role,
    Guid? CompanyId,
    Guid? AreaId);
