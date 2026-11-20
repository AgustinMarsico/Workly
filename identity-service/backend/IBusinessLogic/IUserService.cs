namespace IBusinessLogic;

public interface IUserService
{
    Task<IReadOnlyList<Domain.User>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Domain.User>> GetByCompanyIdAsync(Guid companyId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<IDataAccess.AreaUser>> GetByAreaIdAsync(
        Guid companyId,
        Guid areaId,
        CancellationToken cancellationToken = default);
    Task<Domain.User> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Domain.User> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<Domain.User> CreateAsync(Domain.User user, CancellationToken cancellationToken = default);
}
