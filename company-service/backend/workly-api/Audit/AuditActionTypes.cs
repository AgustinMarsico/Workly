namespace workly_api.Audit;

public static class AuditActionTypes
{
    public const string ClientCompanyCreated = "ClientCompanyCreated";
    public const string UserInvited = "UserInvited";
    public const string UserRegistered = "UserRegistered";
    public const string UserAreaAssigned = "UserAreaAssigned";
    public const string UserAreaModified = "UserAreaModified";
    public const string PhysicalSpaceCreated = "PhysicalSpaceCreated";
    public const string PhysicalSpaceUpdated = "PhysicalSpaceUpdated";
    public const string PhysicalSpaceDeleted = "PhysicalSpaceDeleted";
    public const string ReservationCreated = "ReservationCreated";
    public const string ReservationUpdated = "ReservationUpdated";
    public const string ReservationCancelled = "ReservationCancelled";
    public const string ApiKeyCreated = "ApiKeyCreated";
    public const string ApiKeyRevoked = "ApiKeyRevoked";
}
