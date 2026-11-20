namespace WorklyReservationService.IDataAccess.External;

public sealed record ApiKeyValidationResponse(
    bool IsValid,
    Guid UserId,
    string Role,
    Guid? CompanyId,
    bool IsActive = true);
