namespace IBusinessLogic.Rooms;

public record CreateRoomRequest(
    string Name,
    string TypeKey,
    int MaxCapacity,
    string? Equipment);
