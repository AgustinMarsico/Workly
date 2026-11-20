using Domain;

namespace IDataAccess;

public interface IUserRepository
{
    Task<IReadOnlyList<User>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<User>> GetByCompanyIdAsync(Guid companyId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AreaUser>> GetByAreaIdAsync(
        Guid companyId,
        Guid areaId,
        CancellationToken cancellationToken = default);

    Task<User?> GetByIdOrNullAsync(Guid id, CancellationToken cancellationToken = default);

    Task<User> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<User?> GetByEmail(string email, CancellationToken cancellationToken = default);
    Task<User> CreateAsync(User user, CancellationToken cancellationToken = default);
    Task<User> CreateUserByInvitation(Guid invitationId, string hashedPassword, CancellationToken cancellationToken = default);
}
