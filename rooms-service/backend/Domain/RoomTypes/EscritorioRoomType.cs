namespace Domain.RoomTypes;

public sealed class EscritorioRoomType : RoomType
{
    public static readonly EscritorioRoomType Instance = new();

    private EscritorioRoomType()
    {
    }

    public override string Key => "Escritorio";

    public override string DisplayName => "Escritorio";
}
