using CustomExceptions;
using Domain;
using IDataAccess;
using Microsoft.EntityFrameworkCore;
using CustomExceptions.InvitationExceptions;
using MySqlConnector;

namespace DataAccess;

public sealed class UserRepository(WorklyDbContext db) : IUserRepository
{
    public async Task<IReadOnlyList<User>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await db.Users
            .AsNoTracking()
            .OrderBy(u => u.Email)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<User>> GetByCompanyIdAsync(
        Guid companyId,
        CancellationToken cancellationToken = default)
    {
        return await db.Users
            .AsNoTracking()
            .Where(u => u.CompanyId == companyId)
            .OrderBy(u => u.Email)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<AreaUser>> GetByAreaIdAsync(
        Guid companyId,
        Guid areaId,
        CancellationToken cancellationToken = default)
    {
        return await (
                from user in db.Users.AsNoTracking()
                where user.CompanyId == companyId && user.AreaId == areaId
                join preference in db.UserNotificationPreferences.AsNoTracking()
                    on user.Id equals preference.UserId into preferences
                from preference in preferences.DefaultIfEmpty()
                orderby user.Email
                select new AreaUser(
                    user.Id,
                    user.Email,
                    user.AreaId,
                    preference == null || preference.EmailNotificationsEnabled))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public Task<User?> GetByIdOrNullAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
    }

    public async Task<User> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        User? user = await GetByIdOrNullAsync(id, cancellationToken).ConfigureAwait(false);
        return user ?? throw new UserNotFoundByIdException(id);
    }

    public async Task<User?> GetByEmail(string email, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return null;
        }

        string normalizedEmail = email.Trim().ToLowerInvariant();

        return await db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email == normalizedEmail, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<User> CreateAsync(User user, CancellationToken cancellationToken = default)
    {
        try
        {
            var entry = await db.Users.AddAsync(user, cancellationToken).ConfigureAwait(false);
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return entry.Entity;
        }
        catch (DbUpdateException ex) when (ex.InnerException is MySqlException mysqlEx && mysqlEx.Number == 1062)
        {
            throw new UserAlreadyExistsException(user.Email);
        }
    }

    public async Task<User> CreateUserByInvitation(Guid invitationId, string hashedPassword, CancellationToken cancellationToken = default)
    {
        var invitation = await db.Invitations
            .FirstOrDefaultAsync(i => i.Id == invitationId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new InvitationNotFoundException();

        if (invitation.InvitationStatus != InvitationStatus.used)
            throw new InvitationAlreadyUsedException();

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = invitation.UserEmail.ToLowerInvariant(),
            PasswordHash = hashedPassword,
            CompanyId = invitation.CompanyId,
            Role = UserRoles.Member
        };

        return await CreateAsync(user, cancellationToken).ConfigureAwait(false);
    }
}
