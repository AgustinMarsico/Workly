namespace Domain.RoomTypes;

public sealed class SalaRoomType : RoomType
{
    public static readonly SalaRoomType Instance = new();

    private SalaRoomType()
    {
    }

    public override string Key => "Sala";

    public override string DisplayName => "Sala";
}
