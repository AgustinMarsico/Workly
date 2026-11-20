using Domain;
namespace IDataAccess;

public interface ICompanyRepository
{
    Task<IEnumerable<Company>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Company> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Company> CreateAsync(Company company, CancellationToken cancellationToken = default);
    Task<Area> CreateAreaAsync(Guid companyId, Area area, CancellationToken cancellationToken = default);
    Task<Area> UpdateAreaAsync(Guid companyId, Guid areaId, Area area, CancellationToken cancellationToken = default);
    Task DeleteAreaAsync(Guid companyId, Guid areaId, CancellationToken cancellationToken = default);
}