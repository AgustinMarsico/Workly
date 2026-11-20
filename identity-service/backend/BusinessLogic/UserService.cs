using CustomExceptions;
using Domain;
using IBusinessLogic;
using IDataAccess;

namespace BusinessLogic;

public sealed class UserService(IUserRepository users) : IUserService
{
    public Task<IReadOnlyList<User>> GetAllAsync(CancellationToken cancellationToken = default) =>
        users.GetAllAsync(cancellationToken);

    public Task<IReadOnlyList<User>> GetByCompanyIdAsync(Guid companyId, CancellationToken cancellationToken = default) =>
        users.GetByCompanyIdAsync(companyId, cancellationToken);

    public Task<IReadOnlyList<IDataAccess.AreaUser>> GetByAreaIdAsync(
        Guid companyId,
        Guid areaId,
        CancellationToken cancellationToken = default) =>
        users.GetByAreaIdAsync(companyId, areaId, cancellationToken);

    public Task<User> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        users.GetByIdAsync(id, cancellationToken);

    public async Task<User> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        User? user = await users.GetByEmail(email, cancellationToken).ConfigureAwait(false);
        return user ?? throw new UserNotFoundException(email);
    }

    public async Task<User> CreateAsync(User user, CancellationToken cancellationToken = default)
    {
        if (user is null)
            throw new CreateUserException("User cannot be null.");

        if (user.Id == Guid.Empty)
            user.Id = Guid.NewGuid();

        user.Validate();

        return await users.CreateAsync(user, cancellationToken).ConfigureAwait(false);
    }
}
