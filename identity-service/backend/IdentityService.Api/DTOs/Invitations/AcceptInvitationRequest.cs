namespace workly_api.DTOs.Invitations;

public record AcceptInvitationRequest(
    Guid InvitationId,
    string Token,
    string Password
);
