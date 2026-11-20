using Domain;
using Domain.RoomTypes;
using IBusinessLogic;
using IBusinessLogic.Rooms;
using IDataAccess;

namespace BusinessLogic;

public class RoomService : IRoomService
{
    private readonly IRoomRepository _rooms;

    public RoomService(IRoomRepository rooms)
    {
        _rooms = rooms;
    }

    public Task<IReadOnlyList<RoomTypeOptionDto>> GetRoomTypeOptionsAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        IReadOnlyList<RoomTypeOptionDto> list = RoomTypeFactory.All
            .Select(t => new RoomTypeOptionDto(t.Key, t.DisplayName))
            .ToList();
        return Task.FromResult<IReadOnlyList<RoomTypeOptionDto>>(list);
    }

    public async Task<IReadOnlyList<RoomDto>> GetAllAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Room> rooms = await _rooms.GetAllAsync(companyId, cancellationToken).ConfigureAwait(false);
        return rooms.Select(ToDto).ToList();
    }

    public async Task<RoomDto?> GetByIdAsync(Guid id, Guid companyId, CancellationToken cancellationToken = default)
    {
        Room? room = await _rooms.GetByIdAsync(id, companyId, cancellationToken).ConfigureAwait(false);
        return room is null ? null : ToDto(room);
    }

    public async Task<RoomDto> CreateAsync(
        Guid companyId,
        CreateRoomRequest request,
        CancellationToken cancellationToken = default)
    {
        if (companyId == Guid.Empty)
        {
            throw new ArgumentException("CompanyId cannot be empty.", nameof(companyId));
        }

        ValidateName(request.Name);
        ValidateCapacity(request.MaxCapacity);
        await EnsureUniqueNameAsync(companyId, request.Name, null, cancellationToken).ConfigureAwait(false);
        RoomType type = RoomTypeFactory.GetRequired(request.TypeKey);

        var room = new Room
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            Name = request.Name.Trim(),
            Type = type,
            MaxCapacity = request.MaxCapacity,
            Equipment = string.IsNullOrWhiteSpace(request.Equipment) ? null : request.Equipment.Trim(),
        };

        Room created = await _rooms.AddAsync(room, cancellationToken).ConfigureAwait(false);
        return ToDto(created);
    }

    public async Task<RoomDto?> UpdateAsync(
        Guid id,
        Guid companyId,
        UpdateRoomRequest request,
        CancellationToken cancellationToken = default)
    {
        if (companyId == Guid.Empty)
        {
            throw new ArgumentException("CompanyId cannot be empty.", nameof(companyId));
        }

        ValidateName(request.Name);
        ValidateCapacity(request.MaxCapacity);
        await EnsureUniqueNameAsync(companyId, request.Name, id, cancellationToken).ConfigureAwait(false);
        RoomType type = RoomTypeFactory.GetRequired(request.TypeKey);

        Room? existing = await _rooms.GetByIdAsync(id, companyId, cancellationToken).ConfigureAwait(false);
        if (existing is null)
        {
            return null;
        }

        existing.Name = request.Name.Trim();
        existing.Type = type;
        existing.MaxCapacity = request.MaxCapacity;
        existing.Equipment = string.IsNullOrWhiteSpace(request.Equipment)
            ? null
            : request.Equipment.Trim();

        Room? updated = await _rooms.UpdateAsync(existing, cancellationToken).ConfigureAwait(false);
        return updated is null ? null : ToDto(updated);
    }

    public Task DeleteAsync(Guid id, Guid companyId, CancellationToken cancellationToken = default) =>
        _rooms.DeleteAsync(id, companyId, cancellationToken);

    private static RoomDto ToDto(Room room) =>
        new(
            room.Id,
            room.CompanyId,
            room.Name,
            room.Type.Key,
            room.Type.DisplayName,
            room.MaxCapacity,
            room.Equipment);

    private static void ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("El nombre del espacio es obligatorio.", nameof(name));
        }
    }

    private static void ValidateCapacity(int maxCapacity)
    {
        if (maxCapacity <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maxCapacity),
                "La capacidad máxima debe ser mayor que cero.");
        }
    }

    private async Task EnsureUniqueNameAsync(
        Guid companyId,
        string rawName,
        Guid? excludeRoomId,
        CancellationToken cancellationToken)
    {
        bool exists = await _rooms
            .ExistsByNameAsync(companyId, rawName, excludeRoomId, cancellationToken)
            .ConfigureAwait(false);

        if (exists)
        {
            throw new ArgumentException("Ya existe una sala con ese nombre.", nameof(rawName));
        }
    }
}
