using WorklyReservationService.Domain.CustomExceptions;

namespace WorklyReservationService.Domain.CustomExceptions.ReservationExceptions;

public sealed class ReservationCancelForbiddenException()
    : CustomException(
        "No tiene permiso para cancelar esta reserva.",
        ErrorStatusCode.Forbidden)
{
}
