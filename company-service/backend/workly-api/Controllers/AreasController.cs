using IBusinessLogic;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using workly_api.Auth;
using workly_api.DTOs.Company;

namespace workly_api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Member")]
public sealed class AreasController(ICompanyService companiesService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<CreateAreaToCompanyResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CreateAreaToCompanyResponse>>> GetMine(
        CancellationToken cancellationToken)
    {
        Guid companyId = User.GetCompanyId();
        var company = await companiesService.GetByIdAsync(companyId, cancellationToken);
        return Ok(company.Areas.Select(area => new CreateAreaToCompanyResponse(area)).ToList());
    }
}
