using Domain.RoomTypes;

namespace Domain;

public class Room
{
    public Guid Id { get; set; }

    public Guid CompanyId { get; set; }

    public string Name { get; set; } = string.Empty;

    public RoomType Type { get; set; } = SalaRoomType.Instance;

    public int MaxCapacity { get; set; }

    public string? Equipment { get; set; }
}
