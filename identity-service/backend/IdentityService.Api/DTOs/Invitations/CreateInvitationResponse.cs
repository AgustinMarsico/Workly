using Domain;

namespace workly_api.DTOs.Invitations;

public record CreateInvitationResponse
{
    public Guid Id { get; init; }
    public string UserEmail { get; init; }
    public Guid CompanyId { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime ExpiresAt { get; init; }
    public InvitationStatus InvitationStatus { get; init; }

    public CreateInvitationResponse(Invitation invitation)
    {
        Id = invitation.Id;
        UserEmail = invitation.UserEmail;
        CompanyId = invitation.CompanyId;
        CreatedAt = invitation.CreatedAt;
        ExpiresAt = invitation.ExpiresAt;
        InvitationStatus = invitation.InvitationStatus;
    }
}
