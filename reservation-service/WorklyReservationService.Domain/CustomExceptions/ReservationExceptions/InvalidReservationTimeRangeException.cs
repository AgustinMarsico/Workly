using WorklyReservationService.Domain.CustomExceptions;

namespace WorklyReservationService.Domain.CustomExceptions.ReservationExceptions;

public sealed class InvalidReservationTimeRangeException()
    : CustomException(
        "La hora de fin debe ser posterior a la hora de inicio.",
        ErrorStatusCode.BadRequest)
{
}
