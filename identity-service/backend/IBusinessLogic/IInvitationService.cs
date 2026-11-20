using Domain;
using IBusinessLogic.DTOs;

namespace IBusinessLogic;

public interface IInvitationService
{
    public Task<Invitation> CreateInvitation(CreateInvitationDTO invitationData);
    public Task<(string email, string companyName)> GetInvitationInfoAsync(Guid invitationId);
    public Task<User> AcceptInvitation(Guid invitationId, string token, string password);
}
