using Microsoft.AspNetCore.Mvc;
using IBusinessLogic;
using workly_api.DTOs.Company;
using Microsoft.AspNetCore.Authorization;
using workly_api;
using System.Security.Claims;
using workly_api.Audit;
using workly_api.Auth;

namespace workly_api.Controllers;
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = $"{AuthorizationRoleNames.Admin},{AuthorizationRoleNames.InternalService}")]

public class CompaniesController(
    ICompanyService companiesService,
    IIdentityAreaUsersClient identityAreaUsersClient,
    IAuditEventPublisher? audit = null) : ControllerBase
{
    private readonly IAuditEventPublisher auditPublisher = audit ?? new NoOpAuditEventPublisher();

    [HttpGet]
    public async Task<IActionResult> GetCompanies()
    {
        var companies = await companiesService.GetAllAsync();
        return Ok(companies.Select(c => c.ToResponse()));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetCompany([FromRoute] Guid id)
    {
        var company = await companiesService.GetByIdAsync(id);
        return Ok(company.ToResponse());
    }

    [HttpPost]
    [Authorize(Roles = AuthorizationRoleNames.Admin)]
    public async Task<IActionResult> CreateCompany([FromBody] CreateCompanyRequest request)
    {
        if (request is null)
        {
            return ApiErrors.BadRequest("Request body is required.");
        }

        if (request.Address is null)
        {
            return ApiErrors.BadRequest("Address is required.");
        }

        var company = request.ToEntity();
        company.Validate();
        var createdCompany = await companiesService.CreateAsync(company);
        await auditPublisher.PublishAsync(AuditEventMessage.Create(
            AuditActionTypes.ClientCompanyCreated,
            TryGetUserId(),
            createdCompany.Id,
            "Company",
            createdCompany.Id.ToString(),
            details: new { createdCompany.Name },
            newValues: new { createdCompany.Id, createdCompany.Name, createdCompany.Address },
            correlationId: HttpContext.TraceIdentifier,
            userEmail: User.GetAuditUserEmail()));

        return Ok(createdCompany.ToResponse());
    }

    [HttpPost("{id}/areas")]
    [Authorize(Roles = AuthorizationRoleNames.Admin)]
    public async Task<IActionResult> AddAreaToCompany([FromRoute] Guid id, [FromBody] CreateAreaToCompanyRequest request)
    {
        var area = request.ToEntity();
        var createdArea = await companiesService.CreateAreaAsync(id, area);
        await auditPublisher.PublishAsync(AuditEventMessage.Create(
            AuditActionTypes.UserAreaAssigned,
            TryGetUserId(),
            id,
            "Area",
            createdArea.Id.ToString(),
            details: new { Operation = "AreaCreated" },
            newValues: new { createdArea.Id, createdArea.Name },
            correlationId: HttpContext.TraceIdentifier,
            userEmail: User.GetAuditUserEmail()));

        return Ok(new CreateAreaToCompanyResponse(createdArea));
    }

    [HttpPut("{id}/areas/{areaId}")]
    [Authorize(Roles = AuthorizationRoleNames.Admin)]
    public async Task<IActionResult> UpdateAreaInCompany([FromRoute] Guid id, [FromRoute] Guid areaId, [FromBody] CreateAreaToCompanyRequest request)
    {
        var area = request.ToEntity();
        var previousCompany = await companiesService.GetByIdAsync(id);
        var previousArea = previousCompany.Areas.FirstOrDefault(area => area.Id == areaId);
        var updatedArea = await companiesService.UpdateAreaAsync(id, areaId, area);
        await auditPublisher.PublishAsync(AuditEventMessage.Create(
            AuditActionTypes.UserAreaModified,
            TryGetUserId(),
            id,
            "Area",
            areaId.ToString(),
            details: new { Operation = "AreaUpdated" },
            oldValues: previousArea is null ? null : new { previousArea.Id, previousArea.Name },
            newValues: new { updatedArea.Id, updatedArea.Name },
            correlationId: HttpContext.TraceIdentifier,
            userEmail: User.GetAuditUserEmail()));

        return Ok(new CreateAreaToCompanyResponse(updatedArea));
    }

    [HttpDelete("{id}/areas/{areaId}")]
    [Authorize(Roles = AuthorizationRoleNames.Admin)]
    public async Task<IActionResult> DeleteAreaFromCompany([FromRoute] Guid id, [FromRoute] Guid areaId)
    {
        var company = await companiesService.GetByIdAsync(id);
        var previousArea = company.Areas.FirstOrDefault(area => area.Id == areaId);

        if (previousArea is null)
        {
            return ApiErrors.NotFound("Area was not found.");
        }

        IReadOnlyList<AreaUserResponse> users = await identityAreaUsersClient
            .GetUsersByAreaAsync(id, areaId, HttpContext.RequestAborted)
            .ConfigureAwait(false);

        if (users.Count > 0)
        {
            return ApiErrors.Conflict("No se puede eliminar el área porque tiene usuarios asignados.");
        }

        await companiesService.DeleteAreaAsync(id, areaId);
        await auditPublisher.PublishAsync(AuditEventMessage.Create(
            AuditActionTypes.UserAreaModified,
            TryGetUserId(),
            id,
            "Area",
            areaId.ToString(),
            details: new { Operation = "AreaDeleted" },
            oldValues: new { previousArea.Id, previousArea.Name },
            correlationId: HttpContext.TraceIdentifier,
            userEmail: User.GetAuditUserEmail()));

        return NoContent();
    }

    private Guid? TryGetUserId()
    {
        string? raw = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(raw, out Guid userId) ? userId : null;
    }
}
