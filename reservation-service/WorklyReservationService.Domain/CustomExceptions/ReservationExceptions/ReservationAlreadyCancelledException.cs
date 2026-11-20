using WorklyReservationService.Domain.CustomExceptions;

namespace WorklyReservationService.Domain.CustomExceptions.ReservationExceptions;

public sealed class ReservationAlreadyCancelledException()
    : CustomException("La reserva ya fue cancelada.", ErrorStatusCode.BadRequest)
{
}
