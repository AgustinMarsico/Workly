namespace workly_api.DTOs.Company;

public sealed record AreaUserResponse(
    Guid Id,
    string Email,
    string? Name,
    Guid? AreaId,
    bool EmailSubscribed);
