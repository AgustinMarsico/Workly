namespace workly_api.Audit;

public static class AuditActionTypes
{
    public const string UserInvited = "UserInvited";
    public const string UserRegistered = "UserRegistered";
    public const string UserAreaAssigned = "UserAreaAssigned";
    public const string UserAreaModified = "UserAreaModified";
    public const string ApiKeyCreated = "ApiKeyCreated";
    public const string ApiKeyRevoked = "ApiKeyRevoked";
}
