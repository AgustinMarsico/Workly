using WorklyReservationService.Domain;

namespace WorklyReservationService.IDataAccess;

public interface IAreaNotificationRecipientRepository
{
    Task ReplaceForAreaAsync(
        Guid areaId,
        IReadOnlyList<AreaNotificationRecipient> recipients,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AreaNotificationRecipient>> GetByAreaIdAsync(
        Guid areaId,
        CancellationToken cancellationToken = default);
}
