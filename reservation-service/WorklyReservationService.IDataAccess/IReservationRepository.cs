using WorklyReservationService.Domain;

namespace WorklyReservationService.IDataAccess;

public interface IReservationRepository
{
    Task AddAsync(Reservation reservation, CancellationToken cancellationToken = default);

    Task<Reservation?> GetByIdTrackedAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Reservation>> GetByUserIdAsync(
        Guid userId,
        DateTime? fromUtc,
        DateTime? toUtc,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Reservation>> GetByRoomIdAsync(
        Guid roomId,
        Guid companyId,
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Reservation>> SearchAsync(
        Guid companyId,
        Guid? roomId,
        Guid? userId,
        DateTime? fromUtc,
        DateTime? toUtc,
        string status,
        CancellationToken cancellationToken = default);

    Task<Reservation?> GetByIdAndUserIdAsync(
        Guid reservationId,
        Guid userId,
        Guid companyId,
        CancellationToken cancellationToken = default);

    Task<bool> HasOverlappingActiveAsync(
        Guid roomId,
        DateTime startUtc,
        DateTime endUtc,
        Guid? excludeReservationId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Reservation>> GetUpcomingAsync(
        Guid companyId,
        DateOnly? date,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ReservationTopRoomCount>> GetTopRoomCountsAsync(
        Guid companyId,
        CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
