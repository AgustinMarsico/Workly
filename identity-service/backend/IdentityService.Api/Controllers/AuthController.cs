using Microsoft.AspNetCore.Mvc;
using CustomExceptions;
using IBusinessLogic;
using workly_api.DTOs.Auth;
using workly_api;

namespace workly_api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (request is null)
        {
            return ApiErrors.BadRequest("Request body is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return ApiErrors.BadRequest("Email and password are required.");
        }

        try
        {
            var token = await _authService.Login(request.Email, request.Password);
            return Ok(new { token });
        }
        catch (UnauthorizedException ex)
        {
            return ApiErrors.Unauthorized(ex.Message);
        }
        catch (Exception ex)
        {
            return ApiErrors.InternalServerError($"Login failed: {ex.Message}");
        }
    }
}
