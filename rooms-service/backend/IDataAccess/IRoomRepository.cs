using Domain;

namespace IDataAccess;

public interface IRoomRepository
{
    Task<IReadOnlyList<Room>> GetAllAsync(Guid companyId, CancellationToken cancellationToken = default);

    Task<Room?> GetByIdAsync(Guid id, Guid companyId, CancellationToken cancellationToken = default);

    Task<bool> ExistsByNameAsync(
        Guid companyId,
        string name,
        Guid? excludeRoomId = null,
        CancellationToken cancellationToken = default);

    Task<Room> AddAsync(Room room, CancellationToken cancellationToken = default);

    Task<Room?> UpdateAsync(Room room, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, Guid companyId, CancellationToken cancellationToken = default);
}
