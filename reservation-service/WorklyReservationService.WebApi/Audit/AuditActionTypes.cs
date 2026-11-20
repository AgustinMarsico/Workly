namespace WorklyReservationService.WebApi.Audit;

public static class AuditActionTypes
{
    public const string ReservationCreated = "ReservationCreated";
    public const string ReservationUpdated = "ReservationUpdated";
    public const string ReservationCancelled = "ReservationCancelled";
}
