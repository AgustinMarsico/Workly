using AuditService.Api.Auth;
using AuditService.Api.Contracts;
using AuditService.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;

namespace AuditService.Api.Controllers;

[ApiController]
[Route("api/audit-records")]
[Authorize(Policy = AuditAuthorizationPolicies.AdminOnly)]
public sealed class AuditRecordsController : ControllerBase
{
    private readonly IAuditRecordRepository repository;

    public AuditRecordsController(IAuditRecordRepository repository)
    {
        this.repository = repository;
    }

    [HttpOptions]
    [AllowAnonymous]
    public IActionResult Options()
    {
        AddCorsHeaders();
        return NoContent();
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<AuditRecordResponse>>> Get(
        [FromQuery] AuditRecordsQueryRequest request,
        CancellationToken cancellationToken)
    {
        AddCorsHeaders();

        Guid? callerCompanyId = User.TryGetCompanyId();
        if (callerCompanyId.HasValue
            && request.CompanyId.HasValue
            && request.CompanyId.Value != callerCompanyId.Value)
        {
            return Forbid();
        }

        var query = new AuditRecordQuery(
            request.From,
            request.To,
            request.UserId,
            request.ActionType,
            callerCompanyId ?? request.CompanyId,
            request.Page,
            request.PageSize);

        PagedResult<Domain.AuditRecord> records = await repository.QueryAsync(query, cancellationToken);
        return Ok(new PagedResult<AuditRecordResponse>(
            records.Items.Select(ToResponse).ToList(),
            records.Page,
            records.PageSize,
            records.TotalCount));
    }

    private static AuditRecordResponse ToResponse(Domain.AuditRecord record)
    {
        return new AuditRecordResponse(
            record.Id,
            record.EventId,
            record.OccurredAt,
            record.ActionType,
            record.UserId,
            record.UserEmail,
            record.CompanyId,
            record.EntityType,
            record.EntityId,
            record.DetailsJson,
            record.OldValuesJson,
            record.NewValuesJson,
            record.CorrelationId,
            record.CreatedAt);
    }

    private void AddCorsHeaders()
    {
        StringValues origin = Request.Headers.Origin;
        if (StringValues.IsNullOrEmpty(origin))
        {
            return;
        }

        Response.Headers["Access-Control-Allow-Origin"] = origin.ToString();
        Response.Headers["Access-Control-Allow-Credentials"] = "true";
        Response.Headers["Access-Control-Allow-Methods"] = "GET,OPTIONS";
        Response.Headers["Access-Control-Allow-Headers"] = "authorization,content-type,x-correlation-id";
        Response.Headers["Vary"] = "Origin";
    }
}
