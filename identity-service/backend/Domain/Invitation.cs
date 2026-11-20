namespace Domain;

public sealed class Invitation
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string UserEmail { get; set; }
    public Guid CompanyId { get; set; }
    public Guid AreaId { get; set; }
    public required string TokenHash { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; } = DateTime.UtcNow.AddHours(8);
    public DateTime? UsedAt { get; set; }
    public required Guid CreatedById { get; set; }
    public InvitationStatus InvitationStatus { get; set; } = InvitationStatus.pending;

}