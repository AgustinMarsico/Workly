using CustomExceptions.RoomExceptions;
using Domain;
using IDataAccess;
using Microsoft.EntityFrameworkCore;

namespace DataAccess;

public class RoomRepository : IRoomRepository
{
    private readonly WorklyDbContext _db;

    public RoomRepository(WorklyDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<Room>> GetAllAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        return await _db.Rooms
            .AsNoTracking()
            .Where(r => r.CompanyId == companyId)
            .OrderBy(r => r.Name)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<Room?> GetByIdAsync(Guid id, Guid companyId, CancellationToken cancellationToken = default)
    {
        return await _db.Rooms
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == id && r.CompanyId == companyId, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<bool> ExistsByNameAsync(
        Guid companyId,
        string name,
        Guid? excludeRoomId = null,
        CancellationToken cancellationToken = default)
    {
        string normalizedName = name.Trim().ToLowerInvariant();

        return await _db.Rooms
            .AsNoTracking()
            .Where(r => r.CompanyId == companyId)
            .Where(r => !excludeRoomId.HasValue || r.Id != excludeRoomId.Value)
            .AnyAsync(r => r.Name.ToLower() == normalizedName, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<Room> AddAsync(Room room, CancellationToken cancellationToken = default)
    {
        _db.Rooms.Add(room);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return room;
    }

    public async Task<Room?> UpdateAsync(Room room, CancellationToken cancellationToken = default)
    {
        Room? tracked = await _db.Rooms
            .FirstOrDefaultAsync(r => r.Id == room.Id && r.CompanyId == room.CompanyId, cancellationToken)
            .ConfigureAwait(false);
        if (tracked is null)
        {
            return null;
        }

        tracked.Name = room.Name;
        tracked.Type = room.Type;
        tracked.MaxCapacity = room.MaxCapacity;
        tracked.Equipment = room.Equipment;

        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return tracked;
    }

    public async Task DeleteAsync(
        Guid id,
        Guid companyId,
        CancellationToken cancellationToken = default)
    {
        var roomExists = await _db.Rooms
            .AnyAsync(r => r.Id == id && r.CompanyId == companyId, cancellationToken)
            .ConfigureAwait(false);

        if (!roomExists)
            throw new RoomNotFoundException();

        await _db.Rooms
            .Where(r => r.Id == id && r.CompanyId == companyId)
            .ExecuteDeleteAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}
