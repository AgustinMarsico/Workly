using Domain;
using IBusinessLogic;
using IDataAccess;

namespace BusinessLogic;

public class CompanyService(ICompanyRepository companyRepository) : ICompanyService
{
    public async Task<IEnumerable<Company>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var companies = await companyRepository.GetAllAsync(cancellationToken);
        return companies;
    }
    public async Task<Company> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var company = await companyRepository.GetByIdAsync(id, cancellationToken);
        return company;
    }
    public async Task<Company> CreateAsync(Company company, CancellationToken cancellationToken = default)
    {
        var resultCompany = await companyRepository.CreateAsync(company, cancellationToken);
        return resultCompany;
    }

    public async Task<Area> CreateAreaAsync(Guid companyId, Area area, CancellationToken cancellationToken = default)
    {
        var resultArea = await companyRepository.CreateAreaAsync(companyId, area, cancellationToken);
        return resultArea;
    }

    public async Task<Area> UpdateAreaAsync(Guid companyId, Guid areaId, Area area, CancellationToken cancellationToken = default)
    {
        var resultArea = await companyRepository.UpdateAreaAsync(companyId, areaId, area, cancellationToken);
        return resultArea;
    }

    public async Task DeleteAreaAsync(Guid companyId, Guid areaId, CancellationToken cancellationToken = default)
    {
        await companyRepository.DeleteAreaAsync(companyId, areaId, cancellationToken);
    }
}
