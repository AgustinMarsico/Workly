namespace CustomExceptions.RoomExceptions;

public sealed class RoomNotFoundException() : CustomException(
    "The requested room was not found.",
    ErrorStatusCode.NotFound)
{
}
