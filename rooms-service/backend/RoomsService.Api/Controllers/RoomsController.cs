using IBusinessLogic;
using IBusinessLogic.Rooms;
using Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using workly_api.Auth;
using workly_api;
using workly_api.Audit;
using workly_api.Reservations;

namespace workly_api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Member")]
public class RoomsController : ControllerBase
{
    private readonly IRoomService _rooms;
    private readonly IAuditEventPublisher _audit;
    private readonly IReservationServiceClient _reservationService;

    public RoomsController(
        IRoomService rooms,
        IReservationServiceClient reservationService,
        IAuditEventPublisher? audit = null)
    {
        _rooms = rooms;
        _reservationService = reservationService;
        _audit = audit ?? new NoOpAuditEventPublisher();
    }

    [HttpGet("types")]
    [ProducesResponseType(typeof(IReadOnlyList<RoomTypeOptionDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<RoomTypeOptionDto>>> GetTypes(
        CancellationToken cancellationToken)
    {
        IReadOnlyList<RoomTypeOptionDto> types = await _rooms.GetRoomTypeOptionsAsync(cancellationToken)
            .ConfigureAwait(false);
        return Ok(types);
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<RoomDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<RoomDto>>> GetAll(CancellationToken cancellationToken)
    {
        if (!TryGetCompanyId(out Guid companyId))
        {
            return ApiErrors.Forbidden("Forbidden. You do not have permission to access this resource.");
        }

        IReadOnlyList<RoomDto> list = await _rooms.GetAllAsync(companyId, cancellationToken).ConfigureAwait(false);
        return Ok(list);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(RoomDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RoomDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        if (!TryGetCompanyId(out Guid companyId))
        {
            return ApiErrors.Forbidden("Forbidden. You do not have permission to access this resource.");
        }

        RoomDto? room = await _rooms.GetByIdAsync(id, companyId, cancellationToken).ConfigureAwait(false);
        if (room is null)
        {
            return ApiErrors.NotFound("The requested room was not found.");
        }

        return Ok(room);
    }

    [HttpPost]
    [ProducesResponseType(typeof(RoomDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<RoomDto>> Create(
        [FromBody] CreateRoomRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            if (!TryGetCompanyId(out Guid claimCompanyId))
            {
                return Forbid();
            }
            
            if (request is null)
            {
                return ApiErrors.BadRequest("Request body is required.");
            }

            RoomDto created = await _rooms
                .CreateAsync(claimCompanyId, request, cancellationToken)
                .ConfigureAwait(false);

            await _audit.PublishAsync(AuditEventMessage.Create(
                AuditActionTypes.PhysicalSpaceCreated,
                TryGetUserId(),
                claimCompanyId,
                "Room",
                created.Id.ToString(),
                details: new { Operation = "RoomCreated", request.Name, request.TypeKey, request.MaxCapacity },
                newValues: created,
                correlationId: HttpContext.TraceIdentifier,
                userEmail: User.GetAuditUserEmail()), cancellationToken);

            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch (ArgumentException ex)
        {
            return ApiErrors.BadRequest(ex.Message);
        }
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(RoomDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RoomDto>> Update(
        Guid id,
        [FromBody] UpdateRoomRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            if (request is null)
            {
                return ApiErrors.BadRequest("Request body is required.");
            }

            if (!TryGetCompanyId(out Guid companyId))
            {
                return ApiErrors.Forbidden("Forbidden. You do not have permission to access this resource.");
            }

            RoomDto? previous = await _rooms.GetByIdAsync(id, companyId, cancellationToken)
                .ConfigureAwait(false);
            RoomDto? updated = await _rooms.UpdateAsync(id, companyId, request, cancellationToken)
                .ConfigureAwait(false);
            if (updated is null)
            {
                return ApiErrors.NotFound("The requested room was not found.");
            }

            await _audit.PublishAsync(AuditEventMessage.Create(
                AuditActionTypes.PhysicalSpaceUpdated,
                TryGetUserId(),
                companyId,
                "Room",
                id.ToString(),
                details: new { Operation = "RoomUpdated" },
                oldValues: previous,
                newValues: updated,
                correlationId: HttpContext.TraceIdentifier,
                userEmail: User.GetAuditUserEmail()), cancellationToken);

            return Ok(updated);
        }
        catch (ArgumentException ex)
        {
            return ApiErrors.BadRequest(ex.Message);
        }
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        if (!TryGetCompanyId(out Guid companyId))
        {
            return ApiErrors.Forbidden("Forbidden. You do not have permission to access this resource.");
        }

        RoomDto? previous = await _rooms.GetByIdAsync(id, companyId, cancellationToken)
            .ConfigureAwait(false);
        await _rooms.DeleteAsync(id, companyId, cancellationToken).ConfigureAwait(false);
        await _audit.PublishAsync(AuditEventMessage.Create(
            AuditActionTypes.PhysicalSpaceDeleted,
            TryGetUserId(),
            companyId,
            "Room",
            id.ToString(),
            details: new { Operation = "RoomDeleted" },
            oldValues: previous ?? new RoomDto(id, companyId, string.Empty, string.Empty, string.Empty, 0, null),
            correlationId: HttpContext.TraceIdentifier,
            userEmail: User.GetAuditUserEmail()), cancellationToken);

        return NoContent();
    }

    [HttpGet("top")]
    [ProducesResponseType(typeof(IReadOnlyList<TopRoomDto>), StatusCodes.Status200OK)]
    [Authorize(Roles = "Member")]
    public async Task<ActionResult<IReadOnlyList<TopRoomDto>>> GetTopRooms(CancellationToken cancellationToken)
    {
        if (!TryGetCompanyId(out Guid companyId))
        {
            return ApiErrors.Forbidden("Forbidden. You do not have permission to access this resource.");
        }

        ReservationServiceAuthContext authentication = GetReservationServiceAuthContext();
        IReadOnlyList<TopRoomDto> topRooms = await _reservationService
            .GetTopRoomsAsync(authentication, cancellationToken)
            .ConfigureAwait(false);
        return Ok(topRooms);
    }

    private ReservationServiceAuthContext GetReservationServiceAuthContext()
    {
        string authorization = Request.Headers.Authorization.ToString();
        if (!string.IsNullOrWhiteSpace(authorization)
            && authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return ReservationServiceAuthContext.Bearer(authorization["Bearer ".Length..].Trim());
        }

        string apiKey = Request.Headers["API_KEY"].ToString();
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            apiKey = Request.Headers["X-API-KEY"].ToString();
        }

        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            return ReservationServiceAuthContext.ApiKey(apiKey.Trim());
        }

        throw new UnauthorizedAccessException("No credentials were provided for reservation service forwarding.");
    }

    private bool TryGetCompanyId(out Guid companyId)
    {
        companyId = Guid.Empty;
        string? value = User.FindFirstValue("CompanyId");
        return Guid.TryParse(value, out companyId) && companyId != Guid.Empty;
    }

    private Guid? TryGetUserId()
    {
        string? value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out Guid userId) ? userId : null;
    }
}
