using Microsoft.EntityFrameworkCore;
using WorklyReservationService.Domain;
using WorklyReservationService.IDataAccess;

namespace WorklyReservationService.DataAccess;

public sealed class AreaNotificationRecipientRepository : IAreaNotificationRecipientRepository
{
    private readonly WorklyReservationServiceDbContext _db;

    public AreaNotificationRecipientRepository(WorklyReservationServiceDbContext db)
    {
        _db = db;
    }

    public async Task ReplaceForAreaAsync(
        Guid areaId,
        IReadOnlyList<AreaNotificationRecipient> recipients,
        CancellationToken cancellationToken = default)
    {
        List<AreaNotificationRecipient> existing = await _db.AreaNotificationRecipients
            .Where(recipient => recipient.AreaId == areaId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (existing.Count > 0)
        {
            _db.AreaNotificationRecipients.RemoveRange(existing);
        }

        if (recipients.Count > 0)
        {
            await _db.AreaNotificationRecipients
                .AddRangeAsync(recipients, cancellationToken)
                .ConfigureAwait(false);
        }

        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<AreaNotificationRecipient>> GetByAreaIdAsync(
        Guid areaId,
        CancellationToken cancellationToken = default)
    {
        return await _db.AreaNotificationRecipients
            .AsNoTracking()
            .Where(recipient => recipient.AreaId == areaId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}
