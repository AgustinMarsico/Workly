using IBusinessLogic;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using workly_api.Auth;
using workly_api.DTOs.Company;

namespace workly_api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Member")]
public sealed class UsersController(
    ICompanyService companiesService,
    IIdentityAreaUsersClient identityAreaUsersClient) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<AreaUserResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<AreaUserResponse>>> GetUsersByArea(
        [FromQuery] Guid areaId,
        CancellationToken cancellationToken)
    {
        if (areaId == Guid.Empty)
        {
            return BadRequest("AreaId is required.");
        }

        Guid companyId = User.GetCompanyId();
        var company = await companiesService.GetByIdAsync(companyId, cancellationToken);

        if (!company.Areas.Any(area => area.Id == areaId))
        {
            return NotFound();
        }

        IReadOnlyList<AreaUserResponse> users = await identityAreaUsersClient
            .GetUsersByAreaAsync(areaId, Request, cancellationToken)
            .ConfigureAwait(false);

        return Ok(users);
    }
}
