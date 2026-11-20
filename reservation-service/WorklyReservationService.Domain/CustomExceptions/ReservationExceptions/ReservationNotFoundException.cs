using WorklyReservationService.Domain.CustomExceptions;

namespace WorklyReservationService.Domain.CustomExceptions.ReservationExceptions;

public sealed class ReservationNotFoundException()
    : CustomException("La reserva no fue encontrada.", ErrorStatusCode.NotFound)
{
}
