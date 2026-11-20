using Microsoft.AspNetCore.Mvc.Filters;
using WorklyReservationService.Domain.CustomExceptions;

namespace WorklyReservationService.WebApi.Filters;

public sealed class ExceptionFilter : IExceptionFilter
{
    private readonly ILogger<ExceptionFilter> _logger;

    public ExceptionFilter(ILogger<ExceptionFilter> logger)
    {
        _logger = logger;
    }

    public void OnException(ExceptionContext context)
    {
        var exception = context.Exception;
        if (exception is CustomException customException)
        {
            context.Result = ApiErrors.Create((int)customException.StatusCode, customException.Message);
            context.ExceptionHandled = true;
            return;
        }

        if (exception is UnauthorizedAccessException unauthorizedException)
        {
            context.Result = ApiErrors.Unauthorized(unauthorizedException.Message);
            context.ExceptionHandled = true;
            return;
        }

        _logger.LogError(exception, "Unhandled exception while processing request");
        context.Result ??= ApiErrors.InternalServerError("There was an error when processing the request");
        context.ExceptionHandled = true;
    }
}
