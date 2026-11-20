namespace WorklyReservationService.Domain;

public sealed class AreaNotificationRecipient
{
    public Guid AreaId { get; set; }

    public Guid UserId { get; set; }

    public string Email { get; set; } = string.Empty;

    public string? Name { get; set; }

    public bool EmailSubscribed { get; set; }

    public DateTime UpdatedAtUtc { get; set; }
}
