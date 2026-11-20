using System.Security.Cryptography;
using System.Text;
using CustomExceptions;
using Domain;
using IBusinessLogic;
using IDataAccess;
using CustomExceptions.InvitationExceptions;
using IBusinessLogic.DTOs;
using IBusinessLogic.Organization;

namespace BusinessLogic;

public class InvitationService(
    IInvitationRepository invitationRepository,
    IInvitationNotificationPublisher invitationNotificationPublisher,
    IOrganizationReferenceClient organizationReferences,
    IAuthService authService) : IInvitationService
{
    public async Task<Invitation> CreateInvitation(CreateInvitationDTO invitationData)
    {
        if (string.IsNullOrWhiteSpace(invitationData.UserEmail))
            throw new ArgumentException("User email is required.");

        var normalizedEmail = invitationData.UserEmail.Trim().ToLowerInvariant();
        var organizationReference = await organizationReferences.GetCompanyAreaAsync(
            invitationData.CompanyId,
            invitationData.AreaId);

        var plainToken = GenerateSecureToken();
        var tokenHash = ComputeSha256Hash(plainToken);

        var invitation = new Invitation
        {
            UserEmail = normalizedEmail,
            CompanyId = invitationData.CompanyId,
            TokenHash = tokenHash,
            CreatedById = invitationData.CreatedById,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddHours(8),
            AreaId = invitationData.AreaId,
            InvitationStatus = InvitationStatus.pending
        };

        var createdInvitation = await invitationRepository.CreateInvitation(invitation);

        await invitationNotificationPublisher.PublishAsync(
            new InvitationEmailMessage
            {
                InvitationId = createdInvitation.Id,
                UserEmail = normalizedEmail,
                CompanyId = invitationData.CompanyId,
                CompanyName = organizationReference.CompanyName,
                AreaName = organizationReference.AreaName,
                PlainToken = plainToken,
            });

        return createdInvitation;
    }

    public async Task<(string email, string companyName)> GetInvitationInfoAsync(Guid invitationId)
    {
        var invitation = await invitationRepository.GetInvitationByIdAsync(invitationId)
            ?? throw new InvitationNotFoundException();

        if (invitation.InvitationStatus == InvitationStatus.used)
            throw new InvitationAlreadyUsedException();

        if (DateTime.UtcNow > invitation.ExpiresAt)
            throw new InvitationExpiredException();

        var organizationReference = await organizationReferences.GetCompanyAreaAsync(
            invitation.CompanyId,
            invitation.AreaId);

        return (invitation.UserEmail, organizationReference.CompanyName);
    }

    private static string GenerateSecureToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .Replace("=", "");
    }

    private static string ComputeSha256Hash(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(bytes);
    }

    public async Task<User> AcceptInvitation(Guid invitationId, string token, string password)
    {
        var invitation = await invitationRepository.GetInvitationByIdAsync(invitationId)
            ?? throw new InvitationNotFoundException();

        if (invitation.InvitationStatus == InvitationStatus.used)
            throw new InvitationAlreadyUsedException();

        if (DateTime.UtcNow > invitation.ExpiresAt)
            throw new InvitationExpiredException();

        if (string.IsNullOrWhiteSpace(token))
            throw new InvitationInvalidException("Token de invitación requerido.");

        if (string.IsNullOrWhiteSpace(password))
            throw new InvitationInvalidException("Password de invitación requerido.");

        var submittedHash = ComputeSha256Hash(token);
        if (!string.Equals(invitation.TokenHash, submittedHash, StringComparison.OrdinalIgnoreCase))
            throw new InvitationInvalidException("Token de invitación inválido.");

        var user = await authService.Register(
            invitation.UserEmail,
            password,
            UserRoles.Member,
            invitation.CompanyId,
            invitation.AreaId);

        await invitationRepository.MarkInvitationAsAccepted(invitationId);
        return user;
    }
}
