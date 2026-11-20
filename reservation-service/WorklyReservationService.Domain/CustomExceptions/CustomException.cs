namespace WorklyReservationService.Domain.CustomExceptions;

public abstract class CustomException(string message, ErrorStatusCode statusCode) : Exception(ValidateMessage(message))
{
    public ErrorStatusCode StatusCode { get; } = statusCode;

    private static string ValidateMessage(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            throw new ArgumentException("Exception message cannot be null or empty.");
        }

        return message.Trim();
    }
}
