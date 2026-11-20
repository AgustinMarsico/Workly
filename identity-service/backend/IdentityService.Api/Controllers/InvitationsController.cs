using Microsoft.AspNetCore.Mvc;
using IBusinessLogic;
using workly_api.Auth;
using Domain;
using Microsoft.AspNetCore.Authorization;
using workly_api.DTOs.Invitations;
using workly_api;
using workly_api.Audit;

namespace workly_api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class InvitationsController(
    IInvitationService invitationService,
    IAuditEventPublisher? audit = null) : ControllerBase
{
    private readonly IAuditEventPublisher auditPublisher = audit ?? new NoOpAuditEventPublisher();
    [Authorize]
    [HttpPost]
    public async Task<IActionResult> CreateInvitation([FromBody] CreateInvitationRequest invitationRequest)
    {
        if (invitationRequest is null)
        {
            return ApiErrors.BadRequest("Request body is required.");
        }

        if (string.IsNullOrWhiteSpace(invitationRequest.UserEmail))
        {
            return ApiErrors.BadRequest("UserEmail is required.");
        }

        if (invitationRequest.CompanyId == Guid.Empty)
        {
            return ApiErrors.BadRequest("CompanyId is required.");
        }

        if (invitationRequest.AreaId is null || invitationRequest.AreaId == Guid.Empty)
        {
            return ApiErrors.BadRequest("AreaId is required.");
        }

        Guid adminId = User.GetUserId();

        Invitation invitation = await invitationService
            .CreateInvitation(new IBusinessLogic.DTOs.CreateInvitationDTO
            {
                UserEmail = invitationRequest.UserEmail,
                CompanyId = invitationRequest.CompanyId,
                CreatedById = adminId,
                AreaId = invitationRequest.AreaId.Value
            });

        await auditPublisher.PublishAsync(AuditEventMessage.Create(
            AuditActionTypes.UserInvited,
            adminId,
            invitation.CompanyId,
            "Invitation",
            invitation.Id.ToString(),
            details: new { invitation.UserEmail, invitation.AreaId },
            newValues: new { invitation.Id, invitation.UserEmail, invitation.CompanyId, invitation.AreaId, invitation.InvitationStatus },
            correlationId: HttpContext.TraceIdentifier,
            userEmail: User.GetAuditUserEmail()));

        return Ok(new CreateInvitationResponse(invitation));
    }

    [HttpPost("accept")]
    public async Task<IActionResult> AcceptInvitation([FromBody] AcceptInvitationRequest acceptRequest)
    {
        if (acceptRequest is null)
        {
            return ApiErrors.BadRequest("Request body is required.");
        }

        if (acceptRequest.InvitationId == Guid.Empty)
        {
            return ApiErrors.BadRequest("InvitationId is required.");
        }

        if (string.IsNullOrWhiteSpace(acceptRequest.Token))
        {
            return ApiErrors.BadRequest("Token is required.");
        }

        if (string.IsNullOrWhiteSpace(acceptRequest.Password))
        {
            return ApiErrors.BadRequest("Password is required.");
        }

        User user = await invitationService.AcceptInvitation(
            acceptRequest.InvitationId,
            acceptRequest.Token,
            acceptRequest.Password);

        await auditPublisher.PublishAsync(AuditEventMessage.Create(
            AuditActionTypes.UserRegistered,
            user.Id,
            user.CompanyId,
            "User",
            user.Id.ToString(),
            details: new { acceptRequest.InvitationId },
            newValues: new { user.Id, user.Email, user.Role, user.CompanyId, user.AreaId },
            correlationId: HttpContext.TraceIdentifier,
            userEmail: user.Email));

        return Ok(new { email = user.Email });
    }

    [HttpGet("{invitationId:guid}")]
    public async Task<IActionResult> GetInvitation(Guid invitationId)
    {
        try
        {
            var (email, companyName) = await invitationService.GetInvitationInfoAsync(invitationId);
            return Ok(new { email, companyName });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
