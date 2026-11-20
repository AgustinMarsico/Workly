namespace workly_api.Auth;

public sealed record ApiKeyValidationResult(
    bool IsValid,
    Guid? UserId = null,
    Guid? CompanyId = null,
    string? Role = null,
    bool IsActive = false);
