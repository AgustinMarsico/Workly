namespace CustomExceptions;

public sealed class UserNotFoundByIdException(Guid id)
    : CustomException($"User with id '{id}' not found.", ErrorStatusCode.NotFound)
{
}
