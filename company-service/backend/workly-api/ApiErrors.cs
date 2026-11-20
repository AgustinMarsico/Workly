using Microsoft.AspNetCore.Mvc;

namespace workly_api;

public sealed record ApiErrorResponse(int StatusCode, string Message);

public static class ApiErrors
{
    public static ObjectResult Create(int statusCode, string message)
    {
        return new ObjectResult(new ApiErrorResponse(statusCode, message))
        {
            StatusCode = statusCode
        };
    }

    public static ObjectResult BadRequest(string message) => Create(StatusCodes.Status400BadRequest, message);

    public static ObjectResult Unauthorized(string message) => Create(StatusCodes.Status401Unauthorized, message);

    public static ObjectResult Forbidden(string message) => Create(StatusCodes.Status403Forbidden, message);

    public static ObjectResult NotFound(string message) => Create(StatusCodes.Status404NotFound, message);

    public static ObjectResult Conflict(string message) => Create(StatusCodes.Status409Conflict, message);

    public static ObjectResult InternalServerError(string message) =>
        Create(StatusCodes.Status500InternalServerError, message);
}
