using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WorklyReservationService.IBusinessLogic;
using WorklyReservationService.IBusinessLogic.Reservations;
using WorklyReservationService.IDataAccess;
using WorklyReservationService.IDataAccess.External;
using WorklyReservationService.WebApi.Audit;
using WorklyReservationService.WebApi.Auth;

namespace WorklyReservationService.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Member")]
public sealed class ReservationsController : ControllerBase
{
    private readonly IReservationService _reservations;
    private readonly IAuditEventPublisher _audit;

    public ReservationsController(IReservationService reservations, IAuditEventPublisher? audit = null)
    {
        _reservations = reservations;
        _audit = audit ?? new NoOpAuditEventPublisher();
    }

    [HttpPost]
    [ProducesResponseType(typeof(ReservationDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<ReservationDto>> Create(
        [FromBody] CreateReservationHttpRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            string? message = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .FirstOrDefault(e => !string.IsNullOrWhiteSpace(e));

            return ApiErrors.BadRequest(message ?? "Invalid request body.");
        }

        if (request is null)
        {
            return ApiErrors.BadRequest("Request body is required.");
        }

        if (request.RoomId == Guid.Empty)
        {
            return ApiErrors.BadRequest("RoomId is required.");
        }

        if (request.Start == default)
        {
            return ApiErrors.BadRequest("Start is required.");
        }

        if (request.End == default)
        {
            return ApiErrors.BadRequest("End is required.");
        }

        if (!TryParseUtcTimestamp(request.Start, out DateTimeOffset start)
            || !TryParseUtcTimestamp(request.End, out DateTimeOffset end))
        {
            return ApiErrors.BadRequest("Start and End must be provided in UTC with an explicit offset.");
        }

        ReservationDto created = await _reservations
            .CreateAsync(
                GetMemberUserId(),
                GetMemberCompanyId(),
                GetMainServiceAuthContext(),
                new CreateReservationRequest(request.RoomId, start, end),
                cancellationToken)
            .ConfigureAwait(false);

        await _audit.PublishAsync(AuditEventMessage.Create(
            AuditActionTypes.ReservationCreated,
            GetMemberUserId(),
            GetMemberCompanyId(),
            "Reservation",
            created.Id.ToString(),
            details: new { Operation = "ReservationCreated" },
            newValues: created,
            correlationId: HttpContext.TraceIdentifier,
            userEmail: User.GetAuditUserEmail()), cancellationToken).ConfigureAwait(false);

        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<ReservationDashboardItem>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ReservationDashboardItem>>> GetMine(
        [FromQuery] DateTime? fromUtc,
        [FromQuery] DateTime? toUtc,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<ReservationDashboardItem> list = await _reservations
            .GetMineAsync(GetMemberUserId(), GetMainServiceAuthContext(), fromUtc, toUtc, cancellationToken)
            .ConfigureAwait(false);
        return Ok(list);
    }

    [HttpGet("search")]
    [ProducesResponseType(typeof(PagedResult<ReservationDashboardItem>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<ReservationDashboardItem>>> Search(
        [FromQuery] string? roomId,
        [FromQuery] string? roomType,
        [FromQuery] string? userId,
        [FromQuery] string? areaId,
        [FromQuery] string? fromUtc,
        [FromQuery] string? toUtc,
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        string? roomIdValue = Request.Query.ContainsKey("roomId") ? Request.Query["roomId"].ToString() : roomId;
        if (!TryParseOptionalGuid(roomIdValue, "RoomId", out Guid? parsedRoomId, out ObjectResult? roomIdError))
        {
            return roomIdError!;
        }

        string? userIdValue = Request.Query.ContainsKey("userId") ? Request.Query["userId"].ToString() : userId;
        if (!TryParseOptionalGuid(userIdValue, "UserId", out Guid? parsedUserId, out ObjectResult? userIdError))
        {
            return userIdError!;
        }

        string? areaIdValue = Request.Query.ContainsKey("areaId") ? Request.Query["areaId"].ToString() : areaId;
        if (!TryParseOptionalGuid(areaIdValue, "AreaId", out Guid? parsedAreaId, out ObjectResult? areaIdError))
        {
            return areaIdError!;
        }

        if (!TryParseOptionalUtcTimestamp(fromUtc, "FromUtc", out DateTimeOffset? parsedFromUtc, out ObjectResult? fromError))
        {
            return fromError!;
        }

        if (!TryParseOptionalUtcTimestamp(toUtc, "ToUtc", out DateTimeOffset? parsedToUtc, out ObjectResult? toError))
        {
            return toError!;
        }

        if (parsedFromUtc.HasValue && parsedToUtc.HasValue && parsedToUtc.Value <= parsedFromUtc.Value)
        {
            return ApiErrors.BadRequest("ToUtc must be greater than FromUtc.");
        }

        string normalizedStatus = string.IsNullOrWhiteSpace(status) ? "all" : status.Trim().ToLowerInvariant();
        if (normalizedStatus is not ("active" or "cancelled" or "all"))
        {
            return ApiErrors.BadRequest("Status must be one of: active, cancelled, all.");
        }

        if (page < 1)
        {
            return ApiErrors.BadRequest("Page must be greater than or equal to 1.");
        }

        if (pageSize < 1 || pageSize > 100)
        {
            return ApiErrors.BadRequest("PageSize must be between 1 and 100.");
        }

        PagedResult<ReservationDashboardItem> result = await _reservations
            .SearchAsync(
                GetMemberUserId(),
                GetMemberCompanyId(),
                GetMainServiceAuthContext(),
                new SearchReservationsRequest(
                    parsedRoomId,
                    string.IsNullOrWhiteSpace(roomType) ? null : roomType.Trim(),
                    parsedUserId,
                    parsedAreaId,
                    parsedFromUtc,
                    parsedToUtc,
                    normalizedStatus,
                    page,
                    pageSize),
                cancellationToken)
            .ConfigureAwait(false);

        return Ok(result);
    }

    [HttpGet("upcoming")]
    [ProducesResponseType(typeof(PagedResult<ReservationDashboardItem>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<ReservationDashboardItem>>> GetUpcoming(
        [FromQuery] DateOnly? date,
        [FromQuery] string? roomType,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        PagedResult<ReservationDashboardItem> pageResult = await _reservations
            .GetUpcomingDashboardAsync(
                User.GetCompanyId(),
                GetMainServiceAuthContext(),
                date,
                roomType,
                page,
                pageSize,
                cancellationToken)
            .ConfigureAwait(false);

        return Ok(pageResult);
    }

    [HttpGet("upcoming/stats")]
    [ProducesResponseType(typeof(IReadOnlyList<ReservationRoomTypeCountDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ReservationRoomTypeCountDto>>> GetUpcomingStats(
        [FromQuery] DateOnly? date,
        [FromQuery] string? roomType,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<ReservationRoomTypeCountDto> counts = await _reservations
            .GetUpcomingDashboardCountsAsync(
                User.GetCompanyId(),
                GetMainServiceAuthContext(),
                date,
                roomType,
                cancellationToken)
            .ConfigureAwait(false);

        return Ok(counts);
    }

    [HttpGet("top-rooms")]
    [ProducesResponseType(typeof(IReadOnlyList<ReservationTopRoomDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ReservationTopRoomDto>>> GetTopRooms(
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<ReservationTopRoomDto> topRooms = await _reservations
            .GetTopRoomsAsync(User.GetCompanyId(), GetMainServiceAuthContext(), cancellationToken)
            .ConfigureAwait(false);

        return Ok(topRooms);
    }

    [HttpGet("history")]
    [Authorize(AuthenticationSchemes = ApiKeyAuthenticationOptions.SchemeName, Roles = "Member")]
    [ProducesResponseType(typeof(IReadOnlyList<ReservationDashboardItem>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<ReservationDashboardItem>>> GetHistoryByRoom(
        [FromQuery] Guid roomId,
        [FromQuery] DateTime fromUtc,
        [FromQuery] DateTime toUtc,
        CancellationToken cancellationToken = default)
    {
        if (roomId == Guid.Empty)
        {
            return ApiErrors.BadRequest("RoomId is required.");
        }

        if (fromUtc == default)
        {
            return ApiErrors.BadRequest("FromUtc is required.");
        }

        if (toUtc == default)
        {
            return ApiErrors.BadRequest("ToUtc is required.");
        }

        Guid companyId;
        try
        {
            companyId = User.GetCompanyId();
        }
        catch (UnauthorizedAccessException)
        {
            return ApiErrors.Unauthorized("CompanyId claim not found.");
        }

        IReadOnlyList<ReservationDashboardItem> list = await _reservations
            .GetHistoricalByRoomAsync(
                companyId,
                GetMainServiceAuthContext(),
                roomId,
                fromUtc,
                toUtc,
                cancellationToken)
            .ConfigureAwait(false);
        return Ok(list);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ReservationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ReservationDto>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        ReservationDto? dto = await _reservations
            .GetByIdForUserAsync(id, GetMemberUserId(), GetMemberCompanyId(), cancellationToken)
            .ConfigureAwait(false);
        if (dto is null)
        {
            return ApiErrors.NotFound("The requested reservation was not found.");
        }

        return Ok(dto);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(typeof(ReservationDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ReservationDto>> Cancel(
        Guid id,
        CancellationToken cancellationToken)
    {
        ReservationDto? previous = await _reservations
            .GetByIdForUserAsync(id, GetMemberUserId(), GetMemberCompanyId(), cancellationToken)
            .ConfigureAwait(false);

        ReservationDto dto = await _reservations
            .CancelAsync(id, GetMemberUserId(), GetMemberCompanyId(), GetMainServiceAuthContext(), cancellationToken)
            .ConfigureAwait(false);

        await _audit.PublishAsync(AuditEventMessage.Create(
            AuditActionTypes.ReservationCancelled,
            GetMemberUserId(),
            GetMemberCompanyId(),
            "Reservation",
            dto.Id.ToString(),
            details: new { Operation = "ReservationCancelled" },
            oldValues: previous,
            newValues: dto,
            correlationId: HttpContext.TraceIdentifier,
            userEmail: User.GetAuditUserEmail()), cancellationToken).ConfigureAwait(false);

        return Ok(dto);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ReservationDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ReservationDto>> Update(
        Guid id,
        [FromBody] UpdateReservationHttpRequest request,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return ApiErrors.BadRequest("Request body is required.");
        }

        if (request.RoomId == Guid.Empty)
        {
            return ApiErrors.BadRequest("RoomId is required.");
        }

        if (!TryParseUtcTimestamp(request.Start, out DateTimeOffset start)
            || !TryParseUtcTimestamp(request.End, out DateTimeOffset end))
        {
            return ApiErrors.BadRequest("Start and End must be provided in UTC with an explicit offset.");
        }

        ReservationDto? previous = await _reservations
            .GetByIdForUserAsync(id, GetMemberUserId(), GetMemberCompanyId(), cancellationToken)
            .ConfigureAwait(false);

        ReservationDto dto = await _reservations
            .UpdateAsync(
                id,
                GetMemberUserId(),
                GetMemberCompanyId(),
                GetMainServiceAuthContext(),
                new UpdateReservationRequest(request.RoomId, start, end),
                cancellationToken)
            .ConfigureAwait(false);

        await _audit.PublishAsync(AuditEventMessage.Create(
            AuditActionTypes.ReservationUpdated,
            GetMemberUserId(),
            GetMemberCompanyId(),
            "Reservation",
            dto.Id.ToString(),
            details: new { Operation = "ReservationUpdated" },
            oldValues: previous,
            newValues: dto,
            correlationId: HttpContext.TraceIdentifier,
            userEmail: User.GetAuditUserEmail()), cancellationToken).ConfigureAwait(false);

        return Ok(dto);
    }

    private Guid GetMemberUserId()
    {
        string? sub = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(sub, out Guid userId))
        {
            throw new UnauthorizedAccessException("No se pudo identificar al usuario.");
        }

        return userId;
    }

    private Guid GetMemberCompanyId()
    {
        return User.GetCompanyId();
    }

    private MainServiceAuthContext GetMainServiceAuthContext()
    {
        string authorization = Request.Headers.Authorization.ToString();
        if (!string.IsNullOrWhiteSpace(authorization)
            && authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return MainServiceAuthContext.Bearer(authorization["Bearer ".Length..].Trim());
        }

        string apiKey = Request.Headers["API_KEY"].ToString();
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            apiKey = Request.Headers["X-API-KEY"].ToString();
        }

        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            return MainServiceAuthContext.ApiKey(apiKey.Trim());
        }

        throw new UnauthorizedAccessException("No credentials were provided for main service validation.");
    }

    private static bool TryParseUtcTimestamp(string? value, out DateTimeOffset result)
    {
        result = default;
        return !string.IsNullOrWhiteSpace(value)
            && HasExplicitOffset(value)
            && DateTimeOffset.TryParse(
                value,
                CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind,
                out result)
            && result.Offset == TimeSpan.Zero;
    }

    private static bool TryParseOptionalUtcTimestamp(
        string? value,
        string fieldName,
        out DateTimeOffset? result,
        out ObjectResult? error)
    {
        result = null;
        error = null;

        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        if (!TryParseUtcTimestamp(value, out DateTimeOffset parsed))
        {
            error = ApiErrors.BadRequest($"{fieldName} must be provided in UTC with an explicit offset.");
            return false;
        }

        result = parsed;
        return true;
    }

    private static bool TryParseOptionalGuid(
        string? value,
        string fieldName,
        out Guid? result,
        out ObjectResult? error)
    {
        result = null;
        error = null;

        if (value is null)
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(value) || !Guid.TryParse(value, out Guid parsed) || parsed == Guid.Empty)
        {
            error = ApiErrors.BadRequest($"{fieldName} must be a non-empty GUID.");
            return false;
        }

        result = parsed;
        return true;
    }

    private static bool HasExplicitOffset(string value)
    {
        string trimmed = value.Trim();
        if (trimmed.EndsWith('Z') || trimmed.EndsWith('z'))
        {
            return true;
        }

        return trimmed.Length >= 6
            && (trimmed[^6] == '+' || trimmed[^6] == '-')
            && char.IsDigit(trimmed[^5])
            && char.IsDigit(trimmed[^4])
            && trimmed[^3] == ':'
            && char.IsDigit(trimmed[^2])
            && char.IsDigit(trimmed[^1]);
    }

    public sealed record CreateReservationHttpRequest(Guid RoomId, string? Start, string? End);

    public sealed record UpdateReservationHttpRequest(Guid RoomId, string? Start, string? End);
}
