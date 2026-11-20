namespace CustomExceptions;

public enum ErrorStatusCode
{
    /// <summary>
    /// Indicates that the request could not be understood or was missing required parameters.
    /// </summary>
    BadRequest = 400,

    /// <summary>

    /// <summary>
    /// Indicates that the request requires user authentication.
    /// </summary>
    Unauthorized = 401,

    /// <summary>
    /// Indicates that the server understood the request but refuses to authorize it.
    /// </summary>
    Forbidden = 403,
    /// Indicates that the requested resource could not be found.
    /// </summary>
    NotFound = 404,

    /// <summary>
    /// Indicates that the request could not be completed due to a conflict.
    /// </summary>
    Conflict = 409,

    /// <summary>
    /// Indicates that an unexpected error occurred on the server.
    /// </summary>
    InternalServerError = 500,
}
