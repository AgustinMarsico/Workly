using WorklyReservationService.IBusinessLogic.Reservations;
using WorklyReservationService.IDataAccess;
using WorklyReservationService.IDataAccess.External;

namespace WorklyReservationService.IBusinessLogic;

public interface IReservationService
{
    Task<ReservationDto> CreateAsync(
        Guid memberUserId,
        Guid companyId,
        MainServiceAuthContext authentication,
        CreateReservationRequest request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ReservationDashboardItem>> GetMineAsync(
        Guid memberUserId,
        MainServiceAuthContext authentication,
        DateTime? fromUtc,
        DateTime? toUtc,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ReservationDashboardItem>> GetHistoricalByRoomAsync(
        Guid companyId,
        MainServiceAuthContext authentication,
        Guid roomId,
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken cancellationToken = default);

    Task<PagedResult<ReservationDashboardItem>> SearchAsync(
        Guid memberUserId,
        Guid companyId,
        MainServiceAuthContext authentication,
        SearchReservationsRequest request,
        CancellationToken cancellationToken = default);

    Task<PagedResult<ReservationDashboardItem>> GetUpcomingDashboardAsync(
        Guid companyId,
        MainServiceAuthContext authentication,
        DateOnly? date,
        string? roomType,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ReservationRoomTypeCountDto>> GetUpcomingDashboardCountsAsync(
        Guid companyId,
        MainServiceAuthContext authentication,
        DateOnly? date,
        string? roomType,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ReservationTopRoomDto>> GetTopRoomsAsync(
        Guid companyId,
        MainServiceAuthContext authentication,
        CancellationToken cancellationToken = default);

    Task<ReservationDto?> GetByIdForUserAsync(
        Guid reservationId,
        Guid memberUserId,
        Guid companyId,
        CancellationToken cancellationToken = default);

    Task<ReservationDto> CancelAsync(
        Guid reservationId,
        Guid memberUserId,
        Guid companyId,
        MainServiceAuthContext authentication,
        CancellationToken cancellationToken = default);

    Task<ReservationDto> UpdateAsync(
        Guid reservationId,
        Guid memberUserId,
        Guid companyId,
        MainServiceAuthContext authentication,
        UpdateReservationRequest request,
        CancellationToken cancellationToken = default);
}
