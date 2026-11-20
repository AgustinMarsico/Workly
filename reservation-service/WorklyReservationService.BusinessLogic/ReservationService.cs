using WorklyReservationService.Domain;
using WorklyReservationService.Domain.CustomExceptions.ReservationExceptions;
using WorklyReservationService.Domain.CustomExceptions.RoomExceptions;
using WorklyReservationService.IBusinessLogic;
using WorklyReservationService.IBusinessLogic.Notifications;
using WorklyReservationService.IBusinessLogic.Reservations;
using WorklyReservationService.IDataAccess;
using WorklyReservationService.IDataAccess.External;

namespace WorklyReservationService.BusinessLogic;

public sealed class ReservationService : IReservationService
{
    private readonly IReservationRepository _reservations;
    private readonly IRoomClient _rooms;
    private readonly IUserClient _users;
    private readonly ReservationNotificationService _notifications;

    public ReservationService(
        IReservationRepository reservations,
        IRoomClient rooms,
        IUserClient users,
        ReservationNotificationService notifications)
    {
        _reservations = reservations;
        _rooms = rooms;
        _users = users;
        _notifications = notifications;
    }

    public async Task<ReservationDto> CreateAsync(
        Guid memberUserId,
        Guid companyId,
        MainServiceAuthContext authentication,
        CreateReservationRequest request,
        CancellationToken cancellationToken = default)
    {
        DateTime startUtc = request.Start.UtcDateTime;
        DateTime endUtc = request.End.UtcDateTime;

        if (endUtc <= startUtc)
        {
            throw new InvalidReservationTimeRangeException();
        }

        RoomResponse room = await GetRoomForCompanyAsync(
                request.RoomId,
                companyId,
                authentication,
                cancellationToken)
            .ConfigureAwait(false);

        bool overlap = await _reservations.HasOverlappingActiveAsync(
                room.Id,
                startUtc,
                endUtc,
                excludeReservationId: null,
                cancellationToken)
            .ConfigureAwait(false);

        if (overlap)
        {
            throw new ReservationOverlapException();
        }

        var entity = new Reservation
        {
            Id = Guid.NewGuid(),
            RoomId = room.Id,
            UserId = memberUserId,
            CompanyId = companyId,
            StartUtc = startUtc,
            EndUtc = endUtc,
            CreatedAtUtc = DateTime.UtcNow,
            CancelledAtUtc = null,
            CancelledByUserId = null,
        };

        await _reservations.AddAsync(entity, cancellationToken).ConfigureAwait(false);

        UserResponse actor = await GetRequiredUserAsync(memberUserId, authentication, cancellationToken)
            .ConfigureAwait(false);

        await _notifications.PublishReservationEventAsync(
                "created",
                entity,
                actor,
                room,
                authentication,
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        return ToDto(entity);
    }

    public async Task<IReadOnlyList<ReservationDashboardItem>> GetMineAsync(
        Guid memberUserId,
        MainServiceAuthContext authentication,
        DateTime? fromUtc,
        DateTime? toUtc,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Reservation> list = await _reservations
            .GetByUserIdAsync(memberUserId, fromUtc, toUtc, cancellationToken)
            .ConfigureAwait(false);

        return await ToDashboardItemsAsync(list, authentication, cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<ReservationDashboardItem>> GetHistoricalByRoomAsync(
        Guid companyId,
        MainServiceAuthContext authentication,
        Guid roomId,
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken cancellationToken = default)
    {
        if (toUtc <= fromUtc)
        {
            throw new InvalidReservationTimeRangeException();
        }

        RoomResponse room = await GetRoomForCompanyAsync(roomId, companyId, authentication, cancellationToken)
            .ConfigureAwait(false);

        IReadOnlyList<Reservation> list = await _reservations
            .GetByRoomIdAsync(room.Id, companyId, fromUtc, toUtc, cancellationToken)
            .ConfigureAwait(false);

        return list
            .Select(r => ToDashboardItem(r, room))
            .ToList();
    }

    public async Task<PagedResult<ReservationDashboardItem>> SearchAsync(
        Guid memberUserId,
        Guid companyId,
        MainServiceAuthContext authentication,
        SearchReservationsRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.RoomId.HasValue)
        {
            await GetRoomForCompanyAsync(request.RoomId.Value, companyId, authentication, cancellationToken)
                .ConfigureAwait(false);
        }

        IReadOnlyList<Reservation> matches = await _reservations
            .SearchAsync(
                companyId,
                request.RoomId,
                request.UserId,
                request.FromUtc?.UtcDateTime,
                request.ToUtc?.UtcDateTime,
                request.Status,
                cancellationToken)
            .ConfigureAwait(false);

        IReadOnlyList<ReservationDashboardItem> items = await ToDashboardItemsAsync(
                matches,
                authentication,
                cancellationToken)
            .ConfigureAwait(false);

        if (!string.IsNullOrWhiteSpace(request.RoomType))
        {
            items = items
                .Where(i => string.Equals(i.RoomType, request.RoomType, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        if (request.AreaId.HasValue)
        {
            items = items
                .Where(i => i.UserAreaId == request.AreaId.Value)
                .ToList();
        }

        int page = Math.Max(1, request.Page);
        int pageSize = Math.Clamp(request.PageSize, 1, 100);
        int total = items.Count;
        IReadOnlyList<ReservationDashboardItem> pageItems = items
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new PagedResult<ReservationDashboardItem>(pageItems, total, page, pageSize);
    }

    public async Task<PagedResult<ReservationDashboardItem>> GetUpcomingDashboardAsync(
        Guid companyId,
        MainServiceAuthContext authentication,
        DateOnly? date,
        string? roomType,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        IReadOnlyList<Reservation> reservations = await _reservations
            .GetUpcomingAsync(companyId, date, cancellationToken)
            .ConfigureAwait(false);

        IReadOnlyList<ReservationDashboardItem> items = await ToDashboardItemsAsync(
                reservations,
                authentication,
                cancellationToken)
            .ConfigureAwait(false);

        if (!string.IsNullOrWhiteSpace(roomType))
        {
            items = items
                .Where(i => string.Equals(i.RoomType, roomType, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        int total = items.Count;
        IReadOnlyList<ReservationDashboardItem> pageItems = items
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new PagedResult<ReservationDashboardItem>(pageItems, total, page, pageSize);
    }

    public async Task<IReadOnlyList<ReservationRoomTypeCountDto>> GetUpcomingDashboardCountsAsync(
        Guid companyId,
        MainServiceAuthContext authentication,
        DateOnly? date,
        string? roomType,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Reservation> reservations = await _reservations
            .GetUpcomingAsync(companyId, date, cancellationToken)
            .ConfigureAwait(false);

        IReadOnlyList<ReservationDashboardItem> items = await ToDashboardItemsAsync(
                reservations,
                authentication,
                cancellationToken)
            .ConfigureAwait(false);

        IEnumerable<ReservationDashboardItem> query = items.Where(i => !string.IsNullOrWhiteSpace(i.RoomType));

        if (!string.IsNullOrWhiteSpace(roomType))
        {
            query = query.Where(i => string.Equals(i.RoomType, roomType, StringComparison.OrdinalIgnoreCase));
        }

        return query
            .GroupBy(i => i.RoomType!)
            .Select(group => new ReservationRoomTypeCountDto(group.Key, group.Count()))
            .OrderByDescending(x => x.Count)
            .ThenBy(x => x.RoomTypeKey)
            .ToList();
    }

    public async Task<IReadOnlyList<ReservationTopRoomDto>> GetTopRoomsAsync(
        Guid companyId,
        MainServiceAuthContext authentication,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<ReservationTopRoomCount> counts = await _reservations
            .GetTopRoomCountsAsync(companyId, cancellationToken)
            .ConfigureAwait(false);

        IReadOnlyList<Guid> roomIds = counts.Select(c => c.RoomId).Distinct().ToList();
        IReadOnlyDictionary<Guid, RoomResponse> roomsById = await _rooms
            .GetByIdsAsync(roomIds, authentication, cancellationToken)
            .ConfigureAwait(false);

        return counts
            .Select(count => new
            {
                Count = count,
                Room = roomsById.GetValueOrDefault(count.RoomId)
            })
            .Where(x => x.Room is not null && x.Room.CompanyId == companyId)
            .OrderByDescending(x => x.Count.ReservationsCount)
            .ThenBy(x => x.Room!.Name)
            .Take(3)
            .Select(x => new ReservationTopRoomDto(
                x.Count.RoomId,
                x.Room!.Name,
                x.Count.ReservationsCount,
                companyId))
            .ToList();
    }

    public async Task<ReservationDto?> GetByIdForUserAsync(
        Guid reservationId,
        Guid memberUserId,
        Guid companyId,
        CancellationToken cancellationToken = default)
    {
        Reservation? match = await _reservations
            .GetByIdAndUserIdAsync(reservationId, memberUserId, companyId, cancellationToken)
            .ConfigureAwait(false);

        return match is null ? null : ToDto(match);
    }

    public async Task<ReservationDto> CancelAsync(
        Guid reservationId,
        Guid memberUserId,
        Guid companyId,
        MainServiceAuthContext authentication,
        CancellationToken cancellationToken = default)
    {
        Reservation? entity = await _reservations
            .GetByIdTrackedAsync(reservationId, cancellationToken)
            .ConfigureAwait(false);

        if (entity is null || entity.CompanyId != companyId)
        {
            throw new ReservationNotFoundException();
        }

        if (entity.UserId != memberUserId)
        {
            throw new ReservationCancelForbiddenException();
        }

        if (entity.CancelledAtUtc is not null)
        {
            throw new ReservationAlreadyCancelledException();
        }

        entity.CancelledAtUtc = DateTime.UtcNow;
        entity.CancelledByUserId = memberUserId;

        await _reservations.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        RoomResponse room = await GetRoomForCompanyAsync(
                entity.RoomId,
                companyId,
                authentication,
                cancellationToken)
            .ConfigureAwait(false);
        UserResponse actor = await GetRequiredUserAsync(memberUserId, authentication, cancellationToken)
            .ConfigureAwait(false);

        await _notifications.PublishReservationEventAsync(
                "cancelled",
                entity,
                actor,
                room,
                authentication,
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        return ToDto(entity);
    }

    public async Task<ReservationDto> UpdateAsync(
        Guid reservationId,
        Guid memberUserId,
        Guid companyId,
        MainServiceAuthContext authentication,
        UpdateReservationRequest request,
        CancellationToken cancellationToken = default)
    {
        DateTime startUtc = request.Start.UtcDateTime;
        DateTime endUtc = request.End.UtcDateTime;

        if (endUtc <= startUtc)
        {
            throw new InvalidReservationTimeRangeException();
        }

        Reservation? entity = await _reservations
            .GetByIdTrackedAsync(reservationId, cancellationToken)
            .ConfigureAwait(false);

        if (entity is null || entity.CompanyId != companyId)
        {
            throw new ReservationNotFoundException();
        }

        if (entity.UserId != memberUserId)
        {
            throw new ReservationCancelForbiddenException();
        }

        if (entity.CancelledAtUtc is not null)
        {
            throw new ReservationAlreadyCancelledException();
        }

        RoomResponse room = await GetRoomForCompanyAsync(
                request.RoomId,
                companyId,
                authentication,
                cancellationToken)
            .ConfigureAwait(false);

        DateTime previousStartUtc = entity.StartUtc;
        DateTime previousEndUtc = entity.EndUtc;
        Guid previousRoomId = entity.RoomId;
        RoomResponse? previousRoom = previousRoomId == room.Id
            ? room
            : await _rooms.GetByIdAsync(previousRoomId, authentication, cancellationToken)
                .ConfigureAwait(false);

        bool overlap = await _reservations.HasOverlappingActiveAsync(
                room.Id,
                startUtc,
                endUtc,
                excludeReservationId: entity.Id,
                cancellationToken)
            .ConfigureAwait(false);

        if (overlap)
        {
            throw new ReservationOverlapException();
        }

        entity.RoomId = room.Id;
        entity.StartUtc = startUtc;
        entity.EndUtc = endUtc;

        await _reservations.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var changes = new List<ReservationNotificationChange>();
        if (previousRoomId != room.Id)
        {
            changes.Add(new ReservationNotificationChange
            {
                Field = "space",
                Previous = previousRoom?.Name ?? previousRoomId.ToString(),
                Current = room.Name,
            });
        }

        if (previousStartUtc != startUtc)
        {
            changes.Add(new ReservationNotificationChange
            {
                Field = "startAt",
                Previous = previousStartUtc.ToString("O"),
                Current = startUtc.ToString("O"),
            });
        }

        if (previousEndUtc != endUtc)
        {
            changes.Add(new ReservationNotificationChange
            {
                Field = "endAt",
                Previous = previousEndUtc.ToString("O"),
                Current = endUtc.ToString("O"),
            });
        }

        if (changes.Count == 0)
        {
            return ToDto(entity);
        }

        UserResponse actor = await GetRequiredUserAsync(memberUserId, authentication, cancellationToken)
            .ConfigureAwait(false);

        await _notifications.PublishReservationEventAsync(
                "updated",
                entity,
                actor,
                room,
                authentication,
                changes,
                cancellationToken)
            .ConfigureAwait(false);

        return ToDto(entity);
    }

    private async Task<UserResponse> GetRequiredUserAsync(
        Guid userId,
        MainServiceAuthContext authentication,
        CancellationToken cancellationToken)
    {
        UserResponse? user = await _users.GetByIdAsync(userId, authentication, cancellationToken)
            .ConfigureAwait(false);

        if (user is null)
        {
            throw new UnauthorizedAccessException("User could not be resolved from the identity service.");
        }

        return user;
    }

    private async Task<RoomResponse> GetRoomForCompanyAsync(
        Guid roomId,
        Guid companyId,
        MainServiceAuthContext authentication,
        CancellationToken cancellationToken)
    {
        RoomResponse? room = await _rooms.GetByIdAsync(roomId, authentication, cancellationToken)
            .ConfigureAwait(false);

        if (room is null || room.CompanyId != companyId)
        {
            throw new RoomNotFoundException();
        }

        return room;
    }

    private async Task<IReadOnlyList<ReservationDashboardItem>> ToDashboardItemsAsync(
        IReadOnlyList<Reservation> reservations,
        MainServiceAuthContext authentication,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<Guid> roomIds = reservations.Select(r => r.RoomId).Distinct().ToList();
        IReadOnlyDictionary<Guid, RoomResponse> roomsById = await _rooms
            .GetByIdsAsync(roomIds, authentication, cancellationToken)
            .ConfigureAwait(false);

        IReadOnlyDictionary<Guid, UserResponse> usersById = await GetUsersByIdAsync(
                reservations.Select(r => r.UserId).Distinct().ToList(),
                authentication,
                cancellationToken)
            .ConfigureAwait(false);

        return reservations
            .Select(r => ToDashboardItem(
                r,
                roomsById.GetValueOrDefault(r.RoomId),
                usersById.GetValueOrDefault(r.UserId)))
            .OrderBy(i => i.StartUtc)
            .ThenBy(i => i.RoomName)
            .ToList();
    }

    private async Task<IReadOnlyDictionary<Guid, UserResponse>> GetUsersByIdAsync(
        IReadOnlyCollection<Guid> userIds,
        MainServiceAuthContext authentication,
        CancellationToken cancellationToken)
    {
        if (userIds.Count == 0)
        {
            return new Dictionary<Guid, UserResponse>();
        }

        var results = await Task.WhenAll(userIds.Select(async userId =>
        {
            UserResponse? user = await _users.GetByIdAsync(userId, authentication, cancellationToken)
                .ConfigureAwait(false);
            return user;
        })).ConfigureAwait(false);

        return results
            .Where(user => user is not null)
            .ToDictionary(user => user!.Id, user => user!);
    }

    private static ReservationDashboardItem ToDashboardItem(Reservation r, RoomResponse? room, UserResponse? user = null)
    {
        return new ReservationDashboardItem(
            r.Id,
            r.RoomId,
            room?.Name,
            room?.TypeKey,
            r.UserId,
            user?.Email,
            user?.AreaId,
            new DateTimeOffset(DateTime.SpecifyKind(r.StartUtc, DateTimeKind.Utc)),
            new DateTimeOffset(DateTime.SpecifyKind(r.EndUtc, DateTimeKind.Utc)),
            r.CancelledAtUtc is not null,
            r.CancelledAtUtc is null
                ? null
                : new DateTimeOffset(DateTime.SpecifyKind(r.CancelledAtUtc.Value, DateTimeKind.Utc)),
            r.CancelledByUserId,
            null);
    }

    private static ReservationDto ToDto(Reservation r)
    {
        return new ReservationDto(
            r.Id,
            r.RoomId,
            r.UserId,
            new DateTimeOffset(DateTime.SpecifyKind(r.StartUtc, DateTimeKind.Utc)),
            new DateTimeOffset(DateTime.SpecifyKind(r.EndUtc, DateTimeKind.Utc)),
            r.CancelledAtUtc is not null,
            r.CancelledAtUtc is null
                ? null
                : new DateTimeOffset(DateTime.SpecifyKind(r.CancelledAtUtc.Value, DateTimeKind.Utc)),
            r.CancelledByUserId,
            null);
    }
}
