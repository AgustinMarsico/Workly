using CustomExceptions.AreaExceptions;
using CustomExceptions.CompanyExceptions;
using Domain;
using IDataAccess;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;

namespace DataAccess;

public class CompanyRepository(WorklyDbContext db) : ICompanyRepository
{
    public async Task<IEnumerable<Company>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var companies = await db.Companies
            .AsNoTracking()
            .Include(c => c.Areas)
            .ToListAsync(cancellationToken);
        return companies;
    }
    public async Task<Company> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var company = await db.Companies
            .Include(c => c.Areas)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken)
            ?? throw new CompanyNotFoundException();
        return company;
    }
    public async Task<Company> CreateAsync(Company company, CancellationToken cancellationToken = default)
    {
        try
        {
            var entry = await db.Companies.AddAsync(company, cancellationToken);
            await db.SaveChangesAsync(cancellationToken);
            return entry.Entity;
        }
        catch (DbUpdateException ex) when (ex.InnerException is MySqlException mysqlEx && mysqlEx.Number == 1062)
        {
            throw new CompanyAlreadyExistException();
        }
    }
    public async Task<Area> CreateAreaAsync(Guid companyId, Area area, CancellationToken cancellationToken = default)
    {
        var company = await db.Companies
            .FirstOrDefaultAsync(c => c.Id == companyId, cancellationToken)
            ?? throw new CompanyNotFoundException();

        area.Id = area.Id == Guid.Empty ? Guid.NewGuid() : area.Id;
        area.CompanyId = companyId;

        await db.Set<Area>().AddAsync(area, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return area;
    }
    public async Task<Area> UpdateAreaAsync(Guid companyId, Guid areaId, Area area, CancellationToken cancellationToken = default)
    {
        var company = await db.Companies
            .Include(c => c.Areas)
            .FirstOrDefaultAsync(c => c.Id == companyId, cancellationToken)
            ?? throw new CompanyNotFoundException();

        var areaToUpdate = company.Areas.FirstOrDefault(a => a.Id == areaId)
            ?? throw new AreaNotFoundException();

        areaToUpdate.Name = area.Name;
        areaToUpdate.Description = area.Description;
        areaToUpdate.CompanyId = companyId;

        await db.SaveChangesAsync(cancellationToken);
        return areaToUpdate;
    }
    public async Task DeleteAreaAsync(Guid companyId, Guid areaId, CancellationToken cancellationToken = default)
    {
        var company = await db.Companies
            .Include(c => c.Areas)
            .FirstOrDefaultAsync(c => c.Id == companyId, cancellationToken)
            ?? throw new CompanyNotFoundException();

        var areaToDelete = company.Areas
            .FirstOrDefault(a => a.Id == areaId)
            ?? throw new AreaNotFoundException();

        db.Set<Area>().Remove(areaToDelete);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.InnerException is MySqlException mysqlEx && mysqlEx.Number == 1451)
        {
            // Error 1451: Cannot delete or update a parent row: a foreign key constraint fails
            throw new AreaHasReferencesException();
        }
    }
}
