using System.Security.Claims;
using IBusinessLogic;
using IBusinessLogic.ApiKeys;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using workly_api.Auth;
using workly_api.DTOs.ApiKeys;
using workly_api;
using workly_api.Audit;

namespace workly_api.Controllers;

[ApiController]
[Route("api/api-keys")]
[Authorize(AuthenticationSchemes = "Bearer", Roles = "Member")]
public sealed class ApiKeysController(IApiKeyService apiKeys, IAuditEventPublisher? audit = null) : ControllerBase
{
    private readonly IAuditEventPublisher auditPublisher = audit ?? new NoOpAuditEventPublisher();

    [AllowAnonymous]
    [HttpGet("validate")]
    [ProducesResponseType(typeof(ApiKeyValidationResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiKeyValidationResult>> Validate(CancellationToken cancellationToken)
    {
        string apiKey = Request.Headers["API_KEY"].ToString();
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            apiKey = Request.Headers["X-API-Key"].ToString();
        }

        ApiKeyValidationResult result = await apiKeys
            .ValidateAsync(apiKey, cancellationToken)
            .ConfigureAwait(false);

        if (!result.IsValid || !result.IsActive)
        {
            return Unauthorized(result);
        }

        return Ok(result);
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<ApiKeyListItem>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IReadOnlyList<ApiKeyListItem>>> List(CancellationToken cancellationToken)
    {
        string? userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdValue, out Guid userId) || userId == Guid.Empty)
        {
            return ApiErrors.Unauthorized("Not authorized. Please provide a valid token.");
        }

        Guid companyId;
        try
        {
            companyId = User.GetCompanyId();
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }

        try
        {
            IReadOnlyList<ApiKeyListItem> items = await apiKeys
                .ListAsync(userId, companyId, cancellationToken)
                .ConfigureAwait(false);

            return Ok(items);
        }
        catch (ArgumentException ex)
        {
            return ApiErrors.BadRequest(ex.Message);
        }
    }

    [HttpPost]
    [ProducesResponseType(typeof(CreatedApiKeyResult), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CreatedApiKeyResult>> Create(
        [FromBody] CreateApiKeyRequest request,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return ApiErrors.BadRequest("Request body is required.");
        }

        string? userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdValue, out Guid userId) || userId == Guid.Empty)
        {
            return ApiErrors.Unauthorized("Not authorized. Please provide a valid token.");
        }

        Guid companyId;
        try
        {
            companyId = User.GetCompanyId();
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }

        try
        {
            CreatedApiKeyResult created = await apiKeys
                .CreateAsync(userId, companyId, request.Name, cancellationToken)
                .ConfigureAwait(false);

            await auditPublisher.PublishAsync(AuditEventMessage.Create(
                AuditActionTypes.ApiKeyCreated,
                userId,
                companyId,
                "ApiKey",
                created.Id.ToString(),
                details: new { Operation = "ApiKeyCreated", request.Name },
                newValues: new { created.Id, created.Name, created.CreatedAt },
                correlationId: HttpContext.TraceIdentifier,
                userEmail: User.GetAuditUserEmail()), cancellationToken);

            return Created($"/api/api-keys/{created.Id}", created);
        }
        catch (ArgumentException ex)
        {
            return ApiErrors.BadRequest(ex.Message);
        }
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Revoke(Guid id, CancellationToken cancellationToken)
    {
        string? userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdValue, out Guid userId) || userId == Guid.Empty)
        {
            return ApiErrors.Unauthorized("Not authorized. Please provide a valid token.");
        }

        Guid companyId;
        try
        {
            companyId = User.GetCompanyId();
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }

        bool revoked = await apiKeys.RevokeAsync(userId, companyId, id, cancellationToken).ConfigureAwait(false);
        if (!revoked)
        {
            return ApiErrors.NotFound("The requested API key was not found.");
        }

        await auditPublisher.PublishAsync(AuditEventMessage.Create(
            AuditActionTypes.ApiKeyRevoked,
            userId,
            companyId,
            "ApiKey",
            id.ToString(),
            details: new { Operation = "ApiKeyRevoked" },
            oldValues: new { Id = id, IsActive = true },
            newValues: new { Id = id, IsActive = false },
            correlationId: HttpContext.TraceIdentifier,
            userEmail: User.GetAuditUserEmail()), cancellationToken);

        return NoContent();
    }
}
