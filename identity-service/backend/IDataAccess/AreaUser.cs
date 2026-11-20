namespace IDataAccess;

public sealed record AreaUser(
    Guid Id,
    string Email,
    Guid? AreaId,
    bool EmailSubscribed);
