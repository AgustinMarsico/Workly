namespace CustomExceptions.AreaExceptions;

public sealed class CreateAreaException(string message) : CustomException(message, ErrorStatusCode.BadRequest)
{
}