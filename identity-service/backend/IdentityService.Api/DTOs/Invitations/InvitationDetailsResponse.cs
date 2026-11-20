namespace workly_api.DTOs.Invitations;

public record InvitationDetailsResponse
{
    public Guid Id { get; init; }
    public string UserEmail { get; init; } = string.Empty;
    public Guid CompanyId { get; init; }
    public string CompanyName { get; init; } = string.Empty;
    public DateTime ExpiresAt { get; init; }
    public string InvitationStatus { get; init; } = string.Empty;

    public InvitationDetailsResponse(Domain.Invitation invitation, string companyName)
    {
        Id = invitation.Id;
        UserEmail = invitation.UserEmail;
        CompanyId = invitation.CompanyId;
        CompanyName = companyName;
        ExpiresAt = invitation.ExpiresAt;
        InvitationStatus = invitation.InvitationStatus.ToString();
    }
}
