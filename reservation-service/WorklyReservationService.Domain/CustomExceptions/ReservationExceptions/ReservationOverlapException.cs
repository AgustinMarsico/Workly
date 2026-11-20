using WorklyReservationService.Domain.CustomExceptions;

namespace WorklyReservationService.Domain.CustomExceptions.ReservationExceptions;

public sealed class ReservationOverlapException()
    : CustomException(
        "El horario se superpone con otra reserva activa en el mismo espacio.",
        ErrorStatusCode.Conflict)
{
}
