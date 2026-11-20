namespace IBusinessLogic.ApiKeys;

public sealed record CreatedApiKeyResult(
    Guid Id,
    string Name,
    string ApiKey,
    DateTime CreatedAt
);
