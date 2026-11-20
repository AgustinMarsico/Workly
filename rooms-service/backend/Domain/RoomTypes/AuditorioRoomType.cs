namespace Domain.RoomTypes;

public sealed class AuditorioRoomType : RoomType
{
    public static readonly AuditorioRoomType Instance = new();

    private AuditorioRoomType()
    {
    }

    public override string Key => "Auditorio";

    public override string DisplayName => "Auditorio";
}
