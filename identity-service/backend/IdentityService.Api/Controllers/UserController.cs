using IBusinessLogic;
using IBusinessLogic.Organization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using workly_api.DTOs.User;
using workly_api;
using System.Security.Claims;
using workly_api.Audit;
using workly_api.Auth;
using DataAccess;
using Domain;
using Microsoft.EntityFrameworkCore;
using workly_api.DTOs.Areas;

namespace workly_api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController(
    IUserService userService,
    IOrganizationReferenceClient organizationReferenceClient,
    IPasswordHasherService passwordHasher,
    WorklyDbContext db,
    IAuditEventPublisher? audit = null) : ControllerBase
{
    private readonly IAuditEventPublisher auditPublisher = audit ?? new NoOpAuditEventPublisher();

    [HttpGet]
    [Authorize(Roles = "Admin,Member,InternalService")]
    [ProducesResponseType(typeof(IReadOnlyList<UserResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(IReadOnlyList<AreaUserResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUsers(
        [FromQuery] Guid? areaId,
        [FromQuery(Name = "companyId")] Guid? requestedCompanyId,
        CancellationToken cancellationToken)
    {
        if (areaId.HasValue)
        {
            if (areaId == Guid.Empty)
            {
                return ApiErrors.BadRequest("AreaId cannot be empty.");
            }

            if (User.IsInRole(AuthorizationRoleNames.InternalService))
            {
                if (!requestedCompanyId.HasValue || requestedCompanyId.Value == Guid.Empty)
                {
                    return ApiErrors.BadRequest("CompanyId cannot be empty.");
                }

                await organizationReferenceClient
                    .GetCompanyAreaAsync(requestedCompanyId.Value, areaId.Value, cancellationToken)
                    .ConfigureAwait(false);

                var internalAreaUsers = await userService
                    .GetByAreaIdAsync(requestedCompanyId.Value, areaId.Value, cancellationToken)
                    .ConfigureAwait(false);

                return Ok(internalAreaUsers.Select(user => user.ToResponse()).ToList());
            }

            if (!User.IsInRole("Member"))
            {
                return ApiErrors.Forbidden("Forbidden. You do not have permission to access this resource.");
            }

            Guid companyId;
            try
            {
                companyId = User.GetCompanyId();
            }
            catch (UnauthorizedAccessException)
            {
                return ApiErrors.Forbidden("Forbidden. You do not have permission to access this resource.");
            }

            var requestingUser = await userService
                .GetByIdAsync(User.GetUserId(), cancellationToken)
                .ConfigureAwait(false);

            if (requestingUser.CompanyId != companyId)
            {
                return ApiErrors.Forbidden("Forbidden. You do not have permission to access this resource.");
            }

            await organizationReferenceClient
                .GetCompanyAreaAsync(companyId, areaId.Value, cancellationToken)
                .ConfigureAwait(false);

            var areaUsers = await userService
                .GetByAreaIdAsync(companyId, areaId.Value, cancellationToken)
                .ConfigureAwait(false);

            return Ok(areaUsers.Select(user => user.ToResponse()).ToList());
        }

        if (!User.IsInRole("Admin"))
        {
            return ApiErrors.Forbidden("Forbidden. You do not have permission to access this resource.");
        }

        var users = await userService.GetAllAsync(cancellationToken);
        var emailPreferences = await GetEmailPreferencesAsync(users, cancellationToken);
        return Ok(users.Select(u => u.ToResponse(IsEmailSubscribed(emailPreferences, u.Id))).ToList());
    }

    [HttpGet("company")]
    [Authorize(Roles = "Member")]
    public async Task<IActionResult> GetCompanyUsers(CancellationToken cancellationToken)
    {
        Guid companyId = User.GetCompanyId();
        var users = await userService.GetByCompanyIdAsync(companyId, cancellationToken);
        var emailPreferences = await GetEmailPreferencesAsync(users, cancellationToken);
        return Ok(users.Select(u => u.ToResponse(IsEmailSubscribed(emailPreferences, u.Id))).ToList());
    }

    [HttpGet("{id}")]
    [Authorize(Roles = "Admin,Member")]
    public async Task<IActionResult> GetUser([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var user = await userService.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);

        if (User.IsInRole("Member"))
        {
            Guid companyId;
            try
            {
                companyId = User.GetCompanyId();
            }
            catch (UnauthorizedAccessException)
            {
                return ApiErrors.Forbidden("Forbidden. You do not have permission to access this resource.");
            }

            if (user.CompanyId != companyId)
            {
                return ApiErrors.Forbidden("Forbidden. You do not have permission to access this resource.");
            }
        }

        var emailPreferences = await GetEmailPreferencesAsync([user], CancellationToken.None);
        return Ok(user.ToResponse(IsEmailSubscribed(emailPreferences, user.Id)));
    }

    [HttpGet("me/notification-preferences")]
    [Authorize(Roles = "Member")]
    public async Task<IActionResult> GetMyNotificationPreferences(CancellationToken cancellationToken)
    {
        Guid userId = User.GetUserId();
        UserNotificationPreference? preference = await db.UserNotificationPreferences
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.UserId == userId, cancellationToken);

        return Ok(NotificationPreferencesResponse.From(preference));
    }

    [HttpPut("me/notification-preferences")]
    [Authorize(Roles = "Member")]
    public async Task<IActionResult> UpdateMyNotificationPreferences(
        [FromBody] UpdateNotificationPreferencesRequest request,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return ApiErrors.BadRequest("Request body is required.");
        }

        Guid userId = User.GetUserId();
        UserNotificationPreference? preference = await db.UserNotificationPreferences
            .FirstOrDefaultAsync(item => item.UserId == userId, cancellationToken);

        if (preference is null)
        {
            preference = new UserNotificationPreference
            {
                UserId = userId,
                EmailNotificationsEnabled = request.EmailNotificationsEnabled,
                InAppNotificationsEnabled = true,
            };
            db.UserNotificationPreferences.Add(preference);
        }
        else
        {
            preference.EmailNotificationsEnabled = request.EmailNotificationsEnabled;
            preference.UpdatedAt = DateTime.UtcNow;
        }

        await db.SaveChangesAsync(cancellationToken);
        return Ok(NotificationPreferencesResponse.From(preference));
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request)
    {
        if (request is null)
        {
            return ApiErrors.BadRequest("Request body is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Email) ||
            string.IsNullOrWhiteSpace(request.Password) ||
            string.IsNullOrWhiteSpace(request.Role))
        {
            return ApiErrors.BadRequest("Email, password and role are required.");
        }

        var user = request.ToEntity(passwordHasher);
        var createdUser = await userService.CreateAsync(user);
        await auditPublisher.PublishAsync(AuditEventMessage.Create(
            AuditActionTypes.UserRegistered,
            TryGetUserId(),
            createdUser.CompanyId,
            "User",
            createdUser.Id.ToString(),
            details: new { CreatedBy = "AdminEndpoint" },
            newValues: new { createdUser.Id, createdUser.Email, createdUser.Role, createdUser.CompanyId, createdUser.AreaId },
            correlationId: HttpContext.TraceIdentifier,
            userEmail: User.GetAuditUserEmail()));

        return CreatedAtAction(nameof(GetUser), new { id = createdUser.Id }, createdUser.ToResponse());
    }

    private Guid? TryGetUserId()
    {
        string? raw = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(raw, out Guid userId) ? userId : null;
    }

    private async Task<Dictionary<Guid, bool>> GetEmailPreferencesAsync(
        IReadOnlyList<User> users,
        CancellationToken cancellationToken)
    {
        var userIds = users.Select(user => user.Id).ToList();
        if (userIds.Count == 0)
        {
            return [];
        }

        return await db.UserNotificationPreferences
            .AsNoTracking()
            .Where(preference => userIds.Contains(preference.UserId))
            .ToDictionaryAsync(
                preference => preference.UserId,
                preference => preference.EmailNotificationsEnabled,
                cancellationToken);
    }

    private static bool IsEmailSubscribed(
        IReadOnlyDictionary<Guid, bool> emailPreferences,
        Guid userId) =>
        !emailPreferences.TryGetValue(userId, out bool subscribed) || subscribed;
}

public sealed record UpdateNotificationPreferencesRequest(bool EmailNotificationsEnabled);

public sealed record NotificationPreferencesResponse(
    bool EmailNotificationsEnabled,
    bool InAppNotificationsEnabled)
{
    public static NotificationPreferencesResponse From(UserNotificationPreference? preference) =>
        new(
            preference?.EmailNotificationsEnabled ?? true,
            preference?.InAppNotificationsEnabled ?? true);
}
