using BusinessLogic;
using Domain;
using Domain.RoomTypes;
using IBusinessLogic;
using IBusinessLogic.Rooms;
using IDataAccess;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace BusinessLogicTest;

[TestClass]
public sealed class RoomServiceTests
{
    private Mock<IRoomRepository> _roomRepository = null!;
    private IRoomService _roomService = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _roomRepository = new Mock<IRoomRepository>(MockBehavior.Strict);
        _roomService = new RoomService(_roomRepository.Object);
    }

    [TestMethod]
    public async Task GetRoomTypeOptionsAsync_ReturnsAllRoomTypes()
    {
        IReadOnlyList<RoomTypeOptionDto> result = await _roomService.GetRoomTypeOptionsAsync(CancellationToken.None);

        Assert.AreEqual(3, result.Count);
        Assert.IsTrue(result.Any(t => t.Key == SalaRoomType.Instance.Key && t.DisplayName == SalaRoomType.Instance.DisplayName));
        Assert.IsTrue(result.Any(t => t.Key == EscritorioRoomType.Instance.Key && t.DisplayName == EscritorioRoomType.Instance.DisplayName));
        Assert.IsTrue(result.Any(t => t.Key == AuditorioRoomType.Instance.Key && t.DisplayName == AuditorioRoomType.Instance.DisplayName));
        _roomRepository.VerifyNoOtherCalls();
    }

    [TestMethod]
    public async Task GetRoomTypeOptionsAsync_ThrowsOperationCanceledException_WhenCancellationRequested()
    {
        using var source = new CancellationTokenSource();
        source.Cancel();

        try
        {
            await _roomService.GetRoomTypeOptionsAsync(source.Token);
            Assert.Fail("Expected an OperationCanceledException to be thrown.");
        }
        catch (OperationCanceledException)
        {
            _roomRepository.VerifyNoOtherCalls();
        }
    }

    [TestMethod]
    public async Task GetAllAsync_ReturnsMappedDtos_WhenRepositoryReturnsRooms()
    {
        Guid companyId = Guid.NewGuid();
        IReadOnlyList<Room> rooms =
        [
            CreateRoom(Guid.NewGuid(), companyId, "Sala A", SalaRoomType.Instance, 8, "TV"),
            CreateRoom(Guid.NewGuid(), companyId, "Auditorio", AuditorioRoomType.Instance, 50, null)
        ];
        _roomRepository
            .Setup(r => r.GetAllAsync(companyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(rooms);

        IReadOnlyList<RoomDto> result = await _roomService.GetAllAsync(companyId, CancellationToken.None);

        Assert.AreEqual(2, result.Count);
        AssertRoomDto(rooms[0], result[0]);
        AssertRoomDto(rooms[1], result[1]);
        _roomRepository.VerifyAll();
    }

    [TestMethod]
    public async Task GetAllAsync_ReturnsEmptyList_WhenRepositoryReturnsEmptyList()
    {
        Guid companyId = Guid.NewGuid();
        _roomRepository
            .Setup(r => r.GetAllAsync(companyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        IReadOnlyList<RoomDto> result = await _roomService.GetAllAsync(companyId, CancellationToken.None);

        Assert.AreEqual(0, result.Count);
        _roomRepository.VerifyAll();
    }

    [TestMethod]
    public async Task GetByIdAsync_ReturnsMappedDto_WhenRoomExists()
    {
        Guid companyId = Guid.NewGuid();
        Guid roomId = Guid.NewGuid();
        Room room = CreateRoom(roomId, companyId, "Sala A", EscritorioRoomType.Instance, 1, "Monitor");
        _roomRepository
            .Setup(r => r.GetByIdAsync(roomId, companyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(room);

        RoomDto? result = await _roomService.GetByIdAsync(roomId, companyId, CancellationToken.None);

        Assert.IsNotNull(result);
        AssertRoomDto(room, result);
        _roomRepository.VerifyAll();
    }

    [TestMethod]
    public async Task GetByIdAsync_ReturnsNull_WhenRoomDoesNotExist()
    {
        Guid companyId = Guid.NewGuid();
        Guid roomId = Guid.NewGuid();
        _roomRepository
            .Setup(r => r.GetByIdAsync(roomId, companyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Room?)null);

        RoomDto? result = await _roomService.GetByIdAsync(roomId, companyId, CancellationToken.None);

        Assert.IsNull(result);
        _roomRepository.VerifyAll();
    }

    [TestMethod]
    public async Task CreateAsync_ReturnsMappedDto_AndAddsTrimmedRoom_WhenRequestIsValid()
    {
        Guid companyId = Guid.NewGuid();
        var request = new CreateRoomRequest("  Sala Nueva  ", " sala ", 12, "  TV  ");
        Room? addedRoom = null;
        _roomRepository
            .Setup(r => r.ExistsByNameAsync(companyId, request.Name, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _roomRepository
            .Setup(r => r.AddAsync(It.IsAny<Room>(), It.IsAny<CancellationToken>()))
            .Callback<Room, CancellationToken>((room, _) => addedRoom = room)
            .ReturnsAsync((Room room, CancellationToken _) => room);

        RoomDto result = await _roomService.CreateAsync(companyId, request, CancellationToken.None);

        Assert.IsNotNull(addedRoom);
        Assert.AreNotEqual(Guid.Empty, addedRoom.Id);
        Assert.AreEqual(companyId, addedRoom.CompanyId);
        Assert.AreEqual("Sala Nueva", addedRoom.Name);
        Assert.AreEqual(SalaRoomType.Instance, addedRoom.Type);
        Assert.AreEqual(12, addedRoom.MaxCapacity);
        Assert.AreEqual("TV", addedRoom.Equipment);
        AssertRoomDto(addedRoom, result);
        _roomRepository.VerifyAll();
    }

    [TestMethod]
    public async Task CreateAsync_SetsEquipmentNull_WhenEquipmentIsWhiteSpace()
    {
        Guid companyId = Guid.NewGuid();
        var request = new CreateRoomRequest("Sala Nueva", "Sala", 8, "   ");
        _roomRepository
            .Setup(r => r.ExistsByNameAsync(companyId, request.Name, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _roomRepository
            .Setup(r => r.AddAsync(It.IsAny<Room>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Room room, CancellationToken _) => room);

        RoomDto result = await _roomService.CreateAsync(companyId, request, CancellationToken.None);

        Assert.IsNull(result.Equipment);
        _roomRepository.Verify(r => r.AddAsync(It.Is<Room>(room => room.Equipment == null), It.IsAny<CancellationToken>()), Times.Once);
        _roomRepository.VerifyAll();
    }

    [TestMethod]
    public async Task CreateAsync_ThrowsArgumentException_WhenCompanyIdIsEmpty()
    {
        var request = new CreateRoomRequest("Sala", "Sala", 8, null);

        try
        {
            await _roomService.CreateAsync(Guid.Empty, request, CancellationToken.None);
            Assert.Fail("Expected an ArgumentException to be thrown.");
        }
        catch (ArgumentException ex)
        {
            Assert.AreEqual("CompanyId cannot be empty. (Parameter 'companyId')", ex.Message);
        }

        _roomRepository.Verify(r => r.ExistsByNameAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()), Times.Never);
        _roomRepository.Verify(r => r.AddAsync(It.IsAny<Room>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [TestMethod]
    public async Task CreateAsync_ThrowsArgumentException_WhenNameIsWhiteSpace()
    {
        var request = new CreateRoomRequest("   ", "Sala", 8, null);

        try
        {
            await _roomService.CreateAsync(Guid.NewGuid(), request, CancellationToken.None);
            Assert.Fail("Expected an ArgumentException to be thrown.");
        }
        catch (ArgumentException ex)
        {
            Assert.AreEqual("El nombre del espacio es obligatorio. (Parameter 'name')", ex.Message);
        }

        _roomRepository.Verify(r => r.ExistsByNameAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()), Times.Never);
        _roomRepository.Verify(r => r.AddAsync(It.IsAny<Room>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [TestMethod]
    public async Task CreateAsync_ThrowsArgumentOutOfRangeException_WhenCapacityIsZero()
    {
        var request = new CreateRoomRequest("Sala", "Sala", 0, null);

        try
        {
            await _roomService.CreateAsync(Guid.NewGuid(), request, CancellationToken.None);
            Assert.Fail("Expected an ArgumentOutOfRangeException to be thrown.");
        }
        catch (ArgumentOutOfRangeException ex)
        {
            Assert.AreEqual("maxCapacity", ex.ParamName);
            Assert.IsTrue(ex.Message.Contains("La capacidad máxima debe ser mayor que cero."));
        }

        _roomRepository.Verify(r => r.ExistsByNameAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()), Times.Never);
        _roomRepository.Verify(r => r.AddAsync(It.IsAny<Room>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [TestMethod]
    public async Task CreateAsync_ThrowsArgumentException_WhenRoomNameAlreadyExists()
    {
        Guid companyId = Guid.NewGuid();
        var request = new CreateRoomRequest("Sala", "Sala", 8, null);
        _roomRepository
            .Setup(r => r.ExistsByNameAsync(companyId, request.Name, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        try
        {
            await _roomService.CreateAsync(companyId, request, CancellationToken.None);
            Assert.Fail("Expected an ArgumentException to be thrown.");
        }
        catch (ArgumentException ex)
        {
            Assert.AreEqual("Ya existe una sala con ese nombre. (Parameter 'rawName')", ex.Message);
        }

        _roomRepository.VerifyAll();
        _roomRepository.Verify(r => r.AddAsync(It.IsAny<Room>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [TestMethod]
    public async Task CreateAsync_ThrowsArgumentException_WhenTypeIsInvalid()
    {
        Guid companyId = Guid.NewGuid();
        var request = new CreateRoomRequest("Sala", "Invalido", 8, null);
        _roomRepository
            .Setup(r => r.ExistsByNameAsync(companyId, request.Name, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        try
        {
            await _roomService.CreateAsync(companyId, request, CancellationToken.None);
            Assert.Fail("Expected an ArgumentException to be thrown.");
        }
        catch (ArgumentException ex)
        {
            Assert.AreEqual("key", ex.ParamName);
            Assert.IsTrue(ex.Message.Contains("Tipo de espacio no reconocido: 'Invalido'."));
        }

        _roomRepository.VerifyAll();
        _roomRepository.Verify(r => r.AddAsync(It.IsAny<Room>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [TestMethod]
    public async Task UpdateAsync_ReturnsMappedDto_AndUpdatesExistingRoom_WhenRequestIsValid()
    {
        Guid companyId = Guid.NewGuid();
        Guid roomId = Guid.NewGuid();
        var request = new UpdateRoomRequest("  Sala Editada  ", "Auditorio", 30, "  Proyector  ");
        Room existing = CreateRoom(roomId, companyId, "Sala Vieja", SalaRoomType.Instance, 8, null);
        _roomRepository
            .Setup(r => r.ExistsByNameAsync(companyId, request.Name, roomId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _roomRepository
            .Setup(r => r.GetByIdAsync(roomId, companyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);
        _roomRepository
            .Setup(r => r.UpdateAsync(It.IsAny<Room>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Room room, CancellationToken _) => room);

        RoomDto? result = await _roomService.UpdateAsync(roomId, companyId, request, CancellationToken.None);

        Assert.IsNotNull(result);
        Assert.AreEqual("Sala Editada", existing.Name);
        Assert.AreEqual(AuditorioRoomType.Instance, existing.Type);
        Assert.AreEqual(30, existing.MaxCapacity);
        Assert.AreEqual("Proyector", existing.Equipment);
        AssertRoomDto(existing, result);
        _roomRepository.VerifyAll();
    }

    [TestMethod]
    public async Task UpdateAsync_ReturnsNull_WhenRoomDoesNotExist()
    {
        Guid companyId = Guid.NewGuid();
        Guid roomId = Guid.NewGuid();
        var request = new UpdateRoomRequest("Sala", "Sala", 8, null);
        _roomRepository
            .Setup(r => r.ExistsByNameAsync(companyId, request.Name, roomId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _roomRepository
            .Setup(r => r.GetByIdAsync(roomId, companyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Room?)null);

        RoomDto? result = await _roomService.UpdateAsync(roomId, companyId, request, CancellationToken.None);

        Assert.IsNull(result);
        _roomRepository.VerifyAll();
        _roomRepository.Verify(r => r.UpdateAsync(It.IsAny<Room>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [TestMethod]
    public async Task UpdateAsync_ReturnsNull_WhenRepositoryUpdateReturnsNull()
    {
        Guid companyId = Guid.NewGuid();
        Guid roomId = Guid.NewGuid();
        var request = new UpdateRoomRequest("Sala", "Sala", 8, null);
        Room existing = CreateRoom(roomId, companyId, "Sala Vieja", SalaRoomType.Instance, 4, null);
        _roomRepository
            .Setup(r => r.ExistsByNameAsync(companyId, request.Name, roomId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _roomRepository
            .Setup(r => r.GetByIdAsync(roomId, companyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);
        _roomRepository
            .Setup(r => r.UpdateAsync(existing, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Room?)null);

        RoomDto? result = await _roomService.UpdateAsync(roomId, companyId, request, CancellationToken.None);

        Assert.IsNull(result);
        _roomRepository.VerifyAll();
    }

    [TestMethod]
    public async Task UpdateAsync_SetsEquipmentNull_WhenEquipmentIsWhiteSpace()
    {
        Guid companyId = Guid.NewGuid();
        Guid roomId = Guid.NewGuid();
        var request = new UpdateRoomRequest("Sala", "Sala", 8, "   ");
        Room existing = CreateRoom(roomId, companyId, "Sala Vieja", SalaRoomType.Instance, 4, "TV");
        _roomRepository
            .Setup(r => r.ExistsByNameAsync(companyId, request.Name, roomId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _roomRepository
            .Setup(r => r.GetByIdAsync(roomId, companyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);
        _roomRepository
            .Setup(r => r.UpdateAsync(existing, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        RoomDto? result = await _roomService.UpdateAsync(roomId, companyId, request, CancellationToken.None);

        Assert.IsNotNull(result);
        Assert.IsNull(existing.Equipment);
        Assert.IsNull(result.Equipment);
        _roomRepository.VerifyAll();
    }

    [TestMethod]
    public async Task UpdateAsync_ThrowsArgumentException_WhenCompanyIdIsEmpty()
    {
        var request = new UpdateRoomRequest("Sala", "Sala", 8, null);

        try
        {
            await _roomService.UpdateAsync(Guid.NewGuid(), Guid.Empty, request, CancellationToken.None);
            Assert.Fail("Expected an ArgumentException to be thrown.");
        }
        catch (ArgumentException ex)
        {
            Assert.AreEqual("CompanyId cannot be empty. (Parameter 'companyId')", ex.Message);
        }

        _roomRepository.Verify(r => r.ExistsByNameAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()), Times.Never);
        _roomRepository.Verify(r => r.UpdateAsync(It.IsAny<Room>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [TestMethod]
    public async Task UpdateAsync_ThrowsArgumentException_WhenNameIsWhiteSpace()
    {
        var request = new UpdateRoomRequest("   ", "Sala", 8, null);

        try
        {
            await _roomService.UpdateAsync(Guid.NewGuid(), Guid.NewGuid(), request, CancellationToken.None);
            Assert.Fail("Expected an ArgumentException to be thrown.");
        }
        catch (ArgumentException ex)
        {
            Assert.AreEqual("El nombre del espacio es obligatorio. (Parameter 'name')", ex.Message);
        }

        _roomRepository.Verify(r => r.ExistsByNameAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()), Times.Never);
        _roomRepository.Verify(r => r.UpdateAsync(It.IsAny<Room>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [TestMethod]
    public async Task UpdateAsync_ThrowsArgumentOutOfRangeException_WhenCapacityIsNegative()
    {
        var request = new UpdateRoomRequest("Sala", "Sala", -1, null);

        try
        {
            await _roomService.UpdateAsync(Guid.NewGuid(), Guid.NewGuid(), request, CancellationToken.None);
            Assert.Fail("Expected an ArgumentOutOfRangeException to be thrown.");
        }
        catch (ArgumentOutOfRangeException ex)
        {
            Assert.AreEqual("maxCapacity", ex.ParamName);
            Assert.IsTrue(ex.Message.Contains("La capacidad máxima debe ser mayor que cero."));
        }

        _roomRepository.Verify(r => r.ExistsByNameAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()), Times.Never);
        _roomRepository.Verify(r => r.UpdateAsync(It.IsAny<Room>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [TestMethod]
    public async Task UpdateAsync_ThrowsArgumentException_WhenRoomNameAlreadyExists()
    {
        Guid companyId = Guid.NewGuid();
        Guid roomId = Guid.NewGuid();
        var request = new UpdateRoomRequest("Sala", "Sala", 8, null);
        _roomRepository
            .Setup(r => r.ExistsByNameAsync(companyId, request.Name, roomId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        try
        {
            await _roomService.UpdateAsync(roomId, companyId, request, CancellationToken.None);
            Assert.Fail("Expected an ArgumentException to be thrown.");
        }
        catch (ArgumentException ex)
        {
            Assert.AreEqual("Ya existe una sala con ese nombre. (Parameter 'rawName')", ex.Message);
        }

        _roomRepository.VerifyAll();
        _roomRepository.Verify(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        _roomRepository.Verify(r => r.UpdateAsync(It.IsAny<Room>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [TestMethod]
    public async Task UpdateAsync_ThrowsArgumentException_WhenTypeIsInvalid()
    {
        Guid companyId = Guid.NewGuid();
        Guid roomId = Guid.NewGuid();
        var request = new UpdateRoomRequest("Sala", "Invalido", 8, null);
        _roomRepository
            .Setup(r => r.ExistsByNameAsync(companyId, request.Name, roomId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        try
        {
            await _roomService.UpdateAsync(roomId, companyId, request, CancellationToken.None);
            Assert.Fail("Expected an ArgumentException to be thrown.");
        }
        catch (ArgumentException ex)
        {
            Assert.AreEqual("key", ex.ParamName);
            Assert.IsTrue(ex.Message.Contains("Tipo de espacio no reconocido: 'Invalido'."));
        }

        _roomRepository.VerifyAll();
        _roomRepository.Verify(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        _roomRepository.Verify(r => r.UpdateAsync(It.IsAny<Room>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [TestMethod]
    public async Task DeleteAsync_CallsRepositoryDeleteAsync()
    {
        Guid companyId = Guid.NewGuid();
        Guid roomId = Guid.NewGuid();
        _roomRepository
            .Setup(r => r.DeleteAsync(roomId, companyId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await _roomService.DeleteAsync(roomId, companyId, CancellationToken.None);

        _roomRepository.VerifyAll();
    }

    [TestMethod]
    public async Task DeleteAsync_PropagatesException_WhenRepositoryThrows()
    {
        Guid companyId = Guid.NewGuid();
        Guid roomId = Guid.NewGuid();
        _roomRepository
            .Setup(r => r.DeleteAsync(roomId, companyId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Delete failed."));

        try
        {
            await _roomService.DeleteAsync(roomId, companyId, CancellationToken.None);
            Assert.Fail("Expected an InvalidOperationException to be thrown.");
        }
        catch (InvalidOperationException ex)
        {
            Assert.AreEqual("Delete failed.", ex.Message);
        }

        _roomRepository.VerifyAll();
    }

    private static Room CreateRoom(Guid id, Guid companyId, string name, RoomType type, int maxCapacity, string? equipment) =>
        new()
        {
            Id = id,
            CompanyId = companyId,
            Name = name,
            Type = type,
            MaxCapacity = maxCapacity,
            Equipment = equipment
        };

    private static void AssertRoomDto(Room room, RoomDto dto)
    {
        Assert.AreEqual(room.Id, dto.Id);
        Assert.AreEqual(room.CompanyId, dto.CompanyId);
        Assert.AreEqual(room.Name, dto.Name);
        Assert.AreEqual(room.Type.Key, dto.TypeKey);
        Assert.AreEqual(room.Type.DisplayName, dto.TypeDisplayName);
        Assert.AreEqual(room.MaxCapacity, dto.MaxCapacity);
        Assert.AreEqual(room.Equipment, dto.Equipment);
    }
}
