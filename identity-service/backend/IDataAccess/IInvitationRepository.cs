using Domain;

namespace IDataAccess;

public interface IInvitationRepository
{
    Task<Invitation> CreateInvitation(Invitation invitation, CancellationToken cancellationToken = default);
    Task<Invitation?> GetInvitationByIdAsync(Guid invitationId, CancellationToken cancellationToken = default);
    Task MarkInvitationAsAccepted(Guid invitationId, CancellationToken cancellationToken = default);
    Task<bool> ExistsByAreaIdAsync(Guid areaId, CancellationToken cancellationToken = default);
}