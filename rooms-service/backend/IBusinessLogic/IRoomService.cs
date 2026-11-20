using IBusinessLogic.Rooms;
using Domain;

namespace IBusinessLogic;

public interface IRoomService
{
    Task<IReadOnlyList<RoomTypeOptionDto>> GetRoomTypeOptionsAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RoomDto>> GetAllAsync(Guid companyId, CancellationToken cancellationToken = default);

    Task<RoomDto?> GetByIdAsync(Guid id, Guid companyId, CancellationToken cancellationToken = default);

    Task<RoomDto> CreateAsync(
        Guid companyId,
        CreateRoomRequest request,
        CancellationToken cancellationToken = default);

    Task<RoomDto?> UpdateAsync(Guid id, Guid companyId, UpdateRoomRequest request, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, Guid companyId, CancellationToken cancellationToken = default);
}
