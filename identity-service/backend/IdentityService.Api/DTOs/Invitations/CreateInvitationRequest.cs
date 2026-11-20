namespace workly_api.DTOs.Invitations;

public record CreateInvitationRequest
{
    public string? UserEmail { get; set; }
    public Guid CompanyId { get; set; }
    public Guid? AreaId { get; set; }
}
