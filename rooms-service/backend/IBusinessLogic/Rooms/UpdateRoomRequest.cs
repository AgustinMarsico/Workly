namespace IBusinessLogic.Rooms;

public record UpdateRoomRequest(
    string Name,
    string TypeKey,
    int MaxCapacity,
    string? Equipment);
