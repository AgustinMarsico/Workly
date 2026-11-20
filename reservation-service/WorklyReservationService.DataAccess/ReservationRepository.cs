using Microsoft.EntityFrameworkCore;
using WorklyReservationService.Domain;
using WorklyReservationService.IDataAccess;

namespace WorklyReservationService.DataAccess;

public sealed class ReservationRepository : IReservationRepository
{
    private readonly WorklyReservationServiceDbContext _db;

    public ReservationRepository(WorklyReservationServiceDbContext db)
    {
        _db = db;
    }

    public async Task AddAsync(Reservation reservation, CancellationToken cancellationToken = default)
    {
        _db.Reservations.Add(reservation);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<Reservation?> GetByIdTrackedAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await _db.Reservations
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<Reservation>> GetByUserIdAsync(
        Guid userId,
        DateTime? fromUtc,
        DateTime? toUtc,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Reservation> query = _db.Reservations
            .AsNoTracking()
            .Where(r => r.UserId == userId);

        if (fromUtc.HasValue)
        {
            query = query.Where(r => r.StartUtc >= fromUtc.Value);
        }

        if (toUtc.HasValue)
        {
            query = query.Where(r => r.StartUtc < toUtc.Value);
        }

        return await query
            .OrderBy(r => r.StartUtc)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<Reservation>> GetByRoomIdAsync(
        Guid roomId,
        Guid companyId,
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken cancellationToken = default)
    {
        return await _db.Reservations
            .AsNoTracking()
            .Where(r => r.RoomId == roomId
                        && r.CompanyId == companyId
                        && r.StartUtc >= fromUtc
                        && r.StartUtc < toUtc)
            .OrderBy(r => r.StartUtc)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<Reservation>> SearchAsync(
        Guid companyId,
        Guid? roomId,
        Guid? userId,
        DateTime? fromUtc,
        DateTime? toUtc,
        string status,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Reservation> query = _db.Reservations
            .AsNoTracking()
            .Where(r => r.CompanyId == companyId);

        if (roomId.HasValue)
        {
            query = query.Where(r => r.RoomId == roomId.Value);
        }

        if (userId.HasValue)
        {
            query = query.Where(r => r.UserId == userId.Value);
        }

        if (fromUtc.HasValue)
        {
            query = query.Where(r => r.StartUtc >= fromUtc.Value);
        }

        if (toUtc.HasValue)
        {
            query = query.Where(r => r.StartUtc < toUtc.Value);
        }

        if (string.Equals(status, "active", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(r => r.CancelledAtUtc == null);
        }
        else if (string.Equals(status, "cancelled", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(r => r.CancelledAtUtc != null);
        }

        return await query
            .OrderBy(r => r.StartUtc)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<Reservation?> GetByIdAndUserIdAsync(
        Guid reservationId,
        Guid userId,
        Guid companyId,
        CancellationToken cancellationToken = default)
    {
        return await _db.Reservations
            .AsNoTracking()
            .FirstOrDefaultAsync(
                r => r.Id == reservationId && r.UserId == userId && r.CompanyId == companyId,
                cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<bool> HasOverlappingActiveAsync(
        Guid roomId,
        DateTime startUtc,
        DateTime endUtc,
        Guid? excludeReservationId,
        CancellationToken cancellationToken = default)
    {
        return await _db.Reservations.AnyAsync(
                r => r.RoomId == roomId
                     && r.CancelledAtUtc == null
                     && r.StartUtc < endUtc
                     && r.EndUtc > startUtc
                     && (!excludeReservationId.HasValue || r.Id != excludeReservationId.Value),
                cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<Reservation>> GetUpcomingAsync(
        Guid companyId,
        DateOnly? date,
        CancellationToken cancellationToken = default)
    {
        DateTime windowStartUtc = DateTime.UtcNow.Date;
        DateTime windowEndUtc = windowStartUtc.AddDays(30);

        IQueryable<Reservation> query = _db.Reservations
            .AsNoTracking()
            .Where(r => r.CompanyId == companyId
                        && r.CancelledAtUtc == null
                        && r.StartUtc >= windowStartUtc
                        && r.StartUtc < windowEndUtc);

        if (date.HasValue)
        {
            DateTime dayStartUtc = date.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            DateTime dayEndUtc = dayStartUtc.AddDays(1);

            query = query.Where(r => r.StartUtc >= dayStartUtc && r.StartUtc < dayEndUtc);
        }

        return await query
            .OrderBy(r => r.StartUtc)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<ReservationTopRoomCount>> GetTopRoomCountsAsync(
        Guid companyId,
        CancellationToken cancellationToken = default)
    {
        return await _db.Reservations
            .AsNoTracking()
            .Where(r => r.CompanyId == companyId && r.CancelledAtUtc == null)
            .GroupBy(r => r.RoomId)
            .Select(group => new ReservationTopRoomCount(group.Key, group.Count()))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _db.SaveChangesAsync(cancellationToken);
    }
}
