using CustomExceptions.RoomExceptions;
using DataAccess;
using Domain;
using Domain.RoomTypes;
using IDataAccess;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DataAccessTest;

[TestClass]
public sealed class RoomRepositoryTests
{
    private readonly Guid _companyId = Guid.NewGuid();
    private readonly Guid _otherCompanyId = Guid.NewGuid();
    private SqliteConnection _connection = null!;
    private WorklyDbContext _context = null!;
    private IRoomRepository _repository = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<WorklyDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new WorklyDbContext(options);
        _context.Database.EnsureCreated();
        _repository = new RoomRepository(_context);
    }

    [TestCleanup]
    public void TestCleanup()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
        _connection.Dispose();
    }

    [TestMethod]
    public async Task GetAllAsync_ReturnsOnlyCompanyRooms_OrderedByName()
    {
        Room roomB = CreateRoom(Guid.NewGuid(), _companyId, "Sala B", SalaRoomType.Instance, 8, "TV");
        Room roomA = CreateRoom(Guid.NewGuid(), _companyId, "Sala A", AuditorioRoomType.Instance, 20, null);
        Room otherCompanyRoom = CreateRoom(Guid.NewGuid(), _otherCompanyId, "Sala C", EscritorioRoomType.Instance, 1, "Monitor");
        await LoadRooms(roomB, otherCompanyRoom, roomA);

        IReadOnlyList<Room> result = await _repository.GetAllAsync(_companyId, CancellationToken.None);

        Assert.AreEqual(2, result.Count);
        Assert.AreEqual(roomA.Id, result[0].Id);
        Assert.AreEqual("Sala A", result[0].Name);
        Assert.AreEqual(roomB.Id, result[1].Id);
        Assert.AreEqual("Sala B", result[1].Name);
        Assert.IsFalse(result.Any(r => r.CompanyId == _otherCompanyId));
    }

    [TestMethod]
    public async Task GetAllAsync_ReturnsEmptyList_WhenCompanyHasNoRooms()
    {
        await LoadRooms(CreateRoom(Guid.NewGuid(), _otherCompanyId, "Sala", SalaRoomType.Instance, 8, null));

        IReadOnlyList<Room> result = await _repository.GetAllAsync(_companyId, CancellationToken.None);

        Assert.AreEqual(0, result.Count);
    }

    [TestMethod]
    public async Task GetByIdAsync_ReturnsRoom_WhenRoomBelongsToCompany()
    {
        Room room = CreateRoom(Guid.NewGuid(), _companyId, "Sala", AuditorioRoomType.Instance, 30, "Proyector");
        await LoadRooms(room);

        Room? result = await _repository.GetByIdAsync(room.Id, _companyId, CancellationToken.None);

        Assert.IsNotNull(result);
        AssertRoom(room, result);
    }

    [TestMethod]
    public async Task GetByIdAsync_ReturnsNull_WhenRoomDoesNotExist()
    {
        await LoadRooms(CreateRoom(Guid.NewGuid(), _companyId, "Sala", SalaRoomType.Instance, 8, null));

        Room? result = await _repository.GetByIdAsync(Guid.NewGuid(), _companyId, CancellationToken.None);

        Assert.IsNull(result);
    }

    [TestMethod]
    public async Task GetByIdAsync_ReturnsNull_WhenRoomBelongsToAnotherCompany()
    {
        Room room = CreateRoom(Guid.NewGuid(), _otherCompanyId, "Sala", SalaRoomType.Instance, 8, null);
        await LoadRooms(room);

        Room? result = await _repository.GetByIdAsync(room.Id, _companyId, CancellationToken.None);

        Assert.IsNull(result);
    }

    [TestMethod]
    public async Task ExistsByNameAsync_ReturnsTrue_WhenNameExistsInCompany()
    {
        await LoadRooms(CreateRoom(Guid.NewGuid(), _companyId, "Sala Grande", SalaRoomType.Instance, 8, null));

        bool result = await _repository.ExistsByNameAsync(_companyId, "  sala grande  ", null, CancellationToken.None);

        Assert.IsTrue(result);
    }

    [TestMethod]
    public async Task ExistsByNameAsync_ReturnsFalse_WhenNameDoesNotExist()
    {
        await LoadRooms(CreateRoom(Guid.NewGuid(), _companyId, "Sala Grande", SalaRoomType.Instance, 8, null));

        bool result = await _repository.ExistsByNameAsync(_companyId, "Sala Chica", null, CancellationToken.None);

        Assert.IsFalse(result);
    }

    [TestMethod]
    public async Task ExistsByNameAsync_ReturnsFalse_WhenNameExistsInAnotherCompany()
    {
        await LoadRooms(CreateRoom(Guid.NewGuid(), _otherCompanyId, "Sala Grande", SalaRoomType.Instance, 8, null));

        bool result = await _repository.ExistsByNameAsync(_companyId, "Sala Grande", null, CancellationToken.None);

        Assert.IsFalse(result);
    }

    [TestMethod]
    public async Task ExistsByNameAsync_ExcludesRoomId_WhenExcludeIdIsProvided()
    {
        Room room = CreateRoom(Guid.NewGuid(), _companyId, "Sala Grande", SalaRoomType.Instance, 8, null);
        await LoadRooms(room);

        bool result = await _repository.ExistsByNameAsync(_companyId, "Sala Grande", room.Id, CancellationToken.None);

        Assert.IsFalse(result);
    }

    [TestMethod]
    public async Task ExistsByNameAsync_ReturnsTrue_WhenAnotherRoomHasSameNameAndCurrentRoomIsExcluded()
    {
        Room currentRoom = CreateRoom(Guid.NewGuid(), _companyId, "Sala Grande", SalaRoomType.Instance, 8, null);
        Room anotherRoom = CreateRoom(Guid.NewGuid(), _companyId, "SALA GRANDE", AuditorioRoomType.Instance, 20, null);
        await LoadRooms(currentRoom, anotherRoom);

        bool result = await _repository.ExistsByNameAsync(_companyId, "sala grande", currentRoom.Id, CancellationToken.None);

        Assert.IsTrue(result);
    }

    [TestMethod]
    public async Task AddAsync_PersistsRoom_WhenRoomIsValid()
    {
        Room room = CreateRoom(Guid.NewGuid(), _companyId, "Sala Nueva", EscritorioRoomType.Instance, 1, "Monitor");

        Room result = await _repository.AddAsync(room, CancellationToken.None);

        Assert.AreEqual(room, result);
        Room? persisted = await FindRoomInDb(room.Id);
        Assert.IsNotNull(persisted);
        AssertRoom(room, persisted);
    }

    [TestMethod]
    public async Task UpdateAsync_UpdatesRoom_WhenRoomExists()
    {
        Room existing = CreateRoom(Guid.NewGuid(), _companyId, "Sala Vieja", SalaRoomType.Instance, 8, "TV");
        await LoadRooms(existing);
        Room updated = CreateRoom(existing.Id, _companyId, "Sala Nueva", AuditorioRoomType.Instance, 30, "Proyector");

        Room? result = await _repository.UpdateAsync(updated, CancellationToken.None);

        Assert.IsNotNull(result);
        AssertRoom(updated, result);
        Room? persisted = await FindRoomInDb(existing.Id);
        Assert.IsNotNull(persisted);
        AssertRoom(updated, persisted);
    }

    [TestMethod]
    public async Task UpdateAsync_ReturnsNull_WhenRoomDoesNotExist()
    {
        Room room = CreateRoom(Guid.NewGuid(), _companyId, "Sala", SalaRoomType.Instance, 8, null);

        Room? result = await _repository.UpdateAsync(room, CancellationToken.None);

        Assert.IsNull(result);
    }

    [TestMethod]
    public async Task UpdateAsync_ReturnsNull_WhenRoomBelongsToAnotherCompany()
    {
        Room existing = CreateRoom(Guid.NewGuid(), _otherCompanyId, "Sala Vieja", SalaRoomType.Instance, 8, null);
        await LoadRooms(existing);
        Room updateAttempt = CreateRoom(existing.Id, _companyId, "Sala Nueva", AuditorioRoomType.Instance, 20, null);

        Room? result = await _repository.UpdateAsync(updateAttempt, CancellationToken.None);

        Assert.IsNull(result);
        Room? persisted = await FindRoomInDb(existing.Id);
        Assert.IsNotNull(persisted);
        AssertRoom(existing, persisted);
    }

    [TestMethod]
    public async Task DeleteAsync_RemovesRoom_WhenRoomExists()
    {
        Room room = CreateRoom(Guid.NewGuid(), _companyId, "Sala", SalaRoomType.Instance, 8, null);
        await LoadRooms(room);

        await _repository.DeleteAsync(room.Id, _companyId, CancellationToken.None);

        Room? persisted = await FindRoomInDb(room.Id);
        Assert.IsNull(persisted);
    }

    [TestMethod]
    public async Task DeleteAsync_ThrowsRoomNotFoundException_WhenRoomDoesNotExist()
    {
        try
        {
            await _repository.DeleteAsync(Guid.NewGuid(), _companyId, CancellationToken.None);
            Assert.Fail("Expected a RoomNotFoundException to be thrown.");
        }
        catch (RoomNotFoundException ex)
        {
            Assert.AreEqual("The requested room was not found.", ex.Message);
        }
    }

    [TestMethod]
    public async Task DeleteAsync_ThrowsRoomNotFoundException_WhenRoomBelongsToAnotherCompany()
    {
        Room room = CreateRoom(Guid.NewGuid(), _otherCompanyId, "Sala", SalaRoomType.Instance, 8, null);
        await LoadRooms(room);

        try
        {
            await _repository.DeleteAsync(room.Id, _companyId, CancellationToken.None);
            Assert.Fail("Expected a RoomNotFoundException to be thrown.");
        }
        catch (RoomNotFoundException ex)
        {
            Assert.AreEqual("The requested room was not found.", ex.Message);
        }

        Room? persisted = await FindRoomInDb(room.Id);
        Assert.IsNotNull(persisted);
    }

    private async Task LoadRooms(params Room[] rooms)
    {
        _context.Rooms.AddRange(rooms);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();
    }

    private async Task<Room?> FindRoomInDb(Guid id)
    {
        _context.ChangeTracker.Clear();
        return await _context.Rooms.FirstOrDefaultAsync(r => r.Id == id);
    }

    private static Room CreateRoom(
        Guid id,
        Guid companyId,
        string name,
        RoomType type,
        int maxCapacity,
        string? equipment) =>
        new()
        {
            Id = id,
            CompanyId = companyId,
            Name = name,
            Type = type,
            MaxCapacity = maxCapacity,
            Equipment = equipment
        };

    private static void AssertRoom(Room expected, Room actual)
    {
        Assert.AreEqual(expected.Id, actual.Id);
        Assert.AreEqual(expected.CompanyId, actual.CompanyId);
        Assert.AreEqual(expected.Name, actual.Name);
        Assert.AreEqual(expected.Type, actual.Type);
        Assert.AreEqual(expected.Type.Key, actual.Type.Key);
        Assert.AreEqual(expected.MaxCapacity, actual.MaxCapacity);
        Assert.AreEqual(expected.Equipment, actual.Equipment);
    }
}
