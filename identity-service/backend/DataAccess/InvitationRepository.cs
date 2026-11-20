using Domain;
using IDataAccess;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;
using CustomExceptions.InvitationExceptions;

namespace DataAccess;

public class InvitationRepository(WorklyDbContext db) : IInvitationRepository
{
    public async Task<Invitation> CreateInvitation(Invitation invitation, CancellationToken cancellationToken = default)
    {
        var entry = await db.Invitations.AddAsync(invitation, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return entry.Entity;
    }

    public async Task<Invitation?> GetInvitationByIdAsync(Guid invitationId, CancellationToken cancellationToken = default)
    {
        return await db.Invitations
            .FirstOrDefaultAsync(i => i.Id == invitationId, cancellationToken);
    }

    public async Task MarkInvitationAsAccepted(Guid invitationId, CancellationToken cancellationToken = default)
    {
        var invitation = await db.Invitations
            .FirstOrDefaultAsync(i => i.Id == invitationId, cancellationToken)
            ?? throw new InvitationNotFoundException();

        invitation.InvitationStatus = InvitationStatus.used;
        invitation.UsedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> ExistsByAreaIdAsync(Guid areaId, CancellationToken cancellationToken = default)
    {
        return await db.Invitations.AnyAsync(i => i.AreaId == areaId, cancellationToken);
    }
}
