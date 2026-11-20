namespace AuditService.Api.Domain;

public enum AuditActionType
{
    ClientCompanyCreated,
    UserInvited,
    UserRegistered,
    UserAreaAssigned,
    UserAreaModified,
    PhysicalSpaceCreated,
    PhysicalSpaceUpdated,
    PhysicalSpaceDeleted,
    ReservationCreated,
    ReservationUpdated,
    ReservationCancelled,
    ApiKeyCreated,
    ApiKeyRevoked,
    SystemConfigurationChanged
}

public static class AuditActionTypes
{
    public static string Normalize(string actionType)
    {
        if (Enum.TryParse(actionType, ignoreCase: true, out AuditActionType parsed))
        {
            return parsed.ToString();
        }

        throw new ArgumentException($"Unsupported audit action type: {actionType}", nameof(actionType));
    }
}
