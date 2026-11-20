using System.Security.Claims;
using Domain;
using IBusinessLogic;
using IBusinessLogic.Rooms;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using workly_api;
using workly_api.Audit;
using workly_api.Controllers;
using workly_api.Reservations;

namespace RoomsService.ApiTest;

[TestClass]
public sealed class RoomsControllerTests
{
    private readonly Guid _companyId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();
    private Mock<IRoomService> _roomService = null!;
    private Mock<IReservationServiceClient> _reservationService = null!;
    private Mock<IAuditEventPublisher> _audit = null!;
    private RoomsController _controller = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _roomService = new Mock<IRoomService>(MockBehavior.Strict);
        _reservationService = new Mock<IReservationServiceClient>(MockBehavior.Strict);
        _audit = new Mock<IAuditEventPublisher>(MockBehavior.Strict);
        _controller = CreateControllerWithCompanyClaim(_companyId);
    }

    [TestMethod]
    public async Task GetTypes_ReturnsOk_WithRoomTypes()
    {
        IReadOnlyList<RoomTypeOptionDto> types =
        [
            new RoomTypeOptionDto("Sala", "Sala"),
            new RoomTypeOptionDto("Escritorio", "Escritorio"),
            new RoomTypeOptionDto("Auditorio", "Auditorio")
        ];
        _roomService
            .Setup(s => s.GetRoomTypeOptionsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(types);

        ActionResult<IReadOnlyList<RoomTypeOptionDto>> result = await _controller.GetTypes(CancellationToken.None);

        var ok = result.Result as OkObjectResult;
        Assert.IsNotNull(ok);
        Assert.AreEqual(StatusCodes.Status200OK, ok.StatusCode);
        Assert.AreEqual(types, ok.Value);
        _roomService.VerifyAll();
    }

    [TestMethod]
    public async Task GetTypes_ReturnsOk_WhenAuditPublisherIsNotProvided()
    {
        IReadOnlyList<RoomTypeOptionDto> types = [new RoomTypeOptionDto("Sala", "Sala")];
        _roomService
            .Setup(s => s.GetRoomTypeOptionsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(types);
        var controller = new RoomsController(_roomService.Object, _reservationService.Object)
        {
            ControllerContext = _controller.ControllerContext
        };

        ActionResult<IReadOnlyList<RoomTypeOptionDto>> result = await controller.GetTypes(CancellationToken.None);

        var ok = result.Result as OkObjectResult;
        Assert.IsNotNull(ok);
        Assert.AreEqual(types, ok.Value);
        _roomService.VerifyAll();
    }

    [TestMethod]
    public async Task GetAll_ReturnsOk_WhenCompanyClaimIsValid()
    {
        IReadOnlyList<RoomDto> rooms = [CreateRoomDto(Guid.NewGuid(), _companyId, "Sala A")];
        _roomService
            .Setup(s => s.GetAllAsync(_companyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(rooms);

        ActionResult<IReadOnlyList<RoomDto>> result = await _controller.GetAll(CancellationToken.None);

        var ok = result.Result as OkObjectResult;
        Assert.IsNotNull(ok);
        Assert.AreEqual(StatusCodes.Status200OK, ok.StatusCode);
        Assert.AreEqual(rooms, ok.Value);
        _roomService.VerifyAll();
    }

    [TestMethod]
    public async Task GetAll_ReturnsForbidden_WhenCompanyClaimIsMissing()
    {
        _controller = CreateControllerWithoutCompanyClaim();

        ActionResult<IReadOnlyList<RoomDto>> result = await _controller.GetAll(CancellationToken.None);

        AssertForbidden(result.Result);
        _roomService.Verify(s => s.GetAllAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [TestMethod]
    public async Task GetAll_ReturnsForbidden_WhenCompanyClaimIsInvalid()
    {
        _controller = CreateControllerWithCompanyClaim("not-a-guid");

        ActionResult<IReadOnlyList<RoomDto>> result = await _controller.GetAll(CancellationToken.None);

        AssertForbidden(result.Result);
        _roomService.Verify(s => s.GetAllAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [TestMethod]
    public async Task GetAll_ReturnsForbidden_WhenCompanyClaimIsEmptyGuid()
    {
        _controller = CreateControllerWithCompanyClaim(Guid.Empty);

        ActionResult<IReadOnlyList<RoomDto>> result = await _controller.GetAll(CancellationToken.None);

        AssertForbidden(result.Result);
        _roomService.Verify(s => s.GetAllAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [TestMethod]
    public async Task GetById_ReturnsOk_WhenRoomExists()
    {
        Guid roomId = Guid.NewGuid();
        RoomDto room = CreateRoomDto(roomId, _companyId, "Sala A");
        _roomService
            .Setup(s => s.GetByIdAsync(roomId, _companyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(room);

        ActionResult<RoomDto> result = await _controller.GetById(roomId, CancellationToken.None);

        var ok = result.Result as OkObjectResult;
        Assert.IsNotNull(ok);
        Assert.AreEqual(StatusCodes.Status200OK, ok.StatusCode);
        Assert.AreEqual(room, ok.Value);
        _roomService.VerifyAll();
    }

    [TestMethod]
    public async Task GetById_ReturnsNotFound_WhenRoomDoesNotExist()
    {
        Guid roomId = Guid.NewGuid();
        _roomService
            .Setup(s => s.GetByIdAsync(roomId, _companyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((RoomDto?)null);

        ActionResult<RoomDto> result = await _controller.GetById(roomId, CancellationToken.None);

        AssertStatus(result.Result, StatusCodes.Status404NotFound, "The requested room was not found.");
        _roomService.VerifyAll();
    }

    [TestMethod]
    public async Task GetById_ReturnsForbidden_WhenCompanyClaimIsMissing()
    {
        _controller = CreateControllerWithoutCompanyClaim();

        ActionResult<RoomDto> result = await _controller.GetById(Guid.NewGuid(), CancellationToken.None);

        AssertForbidden(result.Result);
        _roomService.Verify(s => s.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [TestMethod]
    public async Task Create_ReturnsCreatedAtAction_WhenRequestIsValid()
    {
        var request = new CreateRoomRequest("Sala Nueva", "Sala", 8, "TV");
        RoomDto created = CreateRoomDto(Guid.NewGuid(), _companyId, request.Name, request.Equipment);
        _roomService
            .Setup(s => s.CreateAsync(_companyId, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(created);
        _audit
            .Setup(a => a.PublishAsync(It.IsAny<AuditEventMessage>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        ActionResult<RoomDto> result = await _controller.Create(request, CancellationToken.None);

        var createdResult = result.Result as CreatedAtActionResult;
        Assert.IsNotNull(createdResult);
        Assert.AreEqual(StatusCodes.Status201Created, createdResult.StatusCode);
        Assert.AreEqual(nameof(RoomsController.GetById), createdResult.ActionName);
        Assert.AreEqual(created, createdResult.Value);
        _roomService.VerifyAll();
        _audit.VerifyAll();
    }

    [TestMethod]
    public async Task Create_ReturnsForbid_WhenCompanyClaimIsMissing()
    {
        _controller = CreateControllerWithoutCompanyClaim();
        var request = new CreateRoomRequest("Sala Nueva", "Sala", 8, null);

        ActionResult<RoomDto> result = await _controller.Create(request, CancellationToken.None);

        Assert.IsInstanceOfType(result.Result, typeof(ForbidResult));
        _roomService.Verify(s => s.CreateAsync(It.IsAny<Guid>(), It.IsAny<CreateRoomRequest>(), It.IsAny<CancellationToken>()), Times.Never);
        _audit.Verify(a => a.PublishAsync(It.IsAny<AuditEventMessage>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [TestMethod]
    public async Task Create_ReturnsBadRequest_WhenRequestIsNull()
    {
        ActionResult<RoomDto> result = await _controller.Create(null!, CancellationToken.None);

        AssertStatus(result.Result, StatusCodes.Status400BadRequest, "Request body is required.");
        _roomService.Verify(s => s.CreateAsync(It.IsAny<Guid>(), It.IsAny<CreateRoomRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [TestMethod]
    public async Task Create_ReturnsBadRequest_WhenServiceThrowsArgumentException()
    {
        var request = new CreateRoomRequest("", "Sala", 8, null);
        _roomService
            .Setup(s => s.CreateAsync(_companyId, request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ArgumentException("El nombre del espacio es obligatorio."));

        ActionResult<RoomDto> result = await _controller.Create(request, CancellationToken.None);

        AssertStatus(result.Result, StatusCodes.Status400BadRequest, "El nombre del espacio es obligatorio.");
        _roomService.VerifyAll();
        _audit.Verify(a => a.PublishAsync(It.IsAny<AuditEventMessage>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [TestMethod]
    public async Task Create_ReturnsCreatedAtAction_WhenUserIdClaimIsInvalid()
    {
        _controller = CreateController(
        [
            new Claim("CompanyId", _companyId.ToString()),
            new Claim(ClaimTypes.NameIdentifier, "not-a-guid")
        ]);
        var request = new CreateRoomRequest("Sala Nueva", "Sala", 8, null);
        RoomDto created = CreateRoomDto(Guid.NewGuid(), _companyId, request.Name);
        _roomService
            .Setup(s => s.CreateAsync(_companyId, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(created);
        _audit
            .Setup(a => a.PublishAsync(It.IsAny<AuditEventMessage>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        ActionResult<RoomDto> result = await _controller.Create(request, CancellationToken.None);

        var createdResult = result.Result as CreatedAtActionResult;
        Assert.IsNotNull(createdResult);
        Assert.AreEqual(created, createdResult.Value);
        _roomService.VerifyAll();
        _audit.VerifyAll();
    }

    [TestMethod]
    public async Task Update_ReturnsOk_WhenRequestIsValid()
    {
        Guid roomId = Guid.NewGuid();
        var request = new UpdateRoomRequest("Sala Editada", "Auditorio", 20, "Proyector");
        RoomDto previous = CreateRoomDto(roomId, _companyId, "Sala Vieja");
        RoomDto updated = CreateRoomDto(roomId, _companyId, request.Name, request.Equipment);
        _roomService
            .Setup(s => s.GetByIdAsync(roomId, _companyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(previous);
        _roomService
            .Setup(s => s.UpdateAsync(roomId, _companyId, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(updated);
        _audit
            .Setup(a => a.PublishAsync(It.IsAny<AuditEventMessage>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        ActionResult<RoomDto> result = await _controller.Update(roomId, request, CancellationToken.None);

        var ok = result.Result as OkObjectResult;
        Assert.IsNotNull(ok);
        Assert.AreEqual(StatusCodes.Status200OK, ok.StatusCode);
        Assert.AreEqual(updated, ok.Value);
        _roomService.VerifyAll();
        _audit.VerifyAll();
    }

    [TestMethod]
    public async Task Update_ReturnsBadRequest_WhenRequestIsNull()
    {
        ActionResult<RoomDto> result = await _controller.Update(Guid.NewGuid(), null!, CancellationToken.None);

        AssertStatus(result.Result, StatusCodes.Status400BadRequest, "Request body is required.");
        _roomService.Verify(s => s.UpdateAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<UpdateRoomRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [TestMethod]
    public async Task Update_ReturnsForbidden_WhenCompanyClaimIsMissing()
    {
        _controller = CreateControllerWithoutCompanyClaim();
        var request = new UpdateRoomRequest("Sala", "Sala", 8, null);

        ActionResult<RoomDto> result = await _controller.Update(Guid.NewGuid(), request, CancellationToken.None);

        AssertForbidden(result.Result);
        _roomService.Verify(s => s.UpdateAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<UpdateRoomRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [TestMethod]
    public async Task Update_ReturnsNotFound_WhenRoomDoesNotExist()
    {
        Guid roomId = Guid.NewGuid();
        var request = new UpdateRoomRequest("Sala", "Sala", 8, null);
        _roomService
            .Setup(s => s.GetByIdAsync(roomId, _companyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((RoomDto?)null);
        _roomService
            .Setup(s => s.UpdateAsync(roomId, _companyId, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync((RoomDto?)null);

        ActionResult<RoomDto> result = await _controller.Update(roomId, request, CancellationToken.None);

        AssertStatus(result.Result, StatusCodes.Status404NotFound, "The requested room was not found.");
        _roomService.VerifyAll();
        _audit.Verify(a => a.PublishAsync(It.IsAny<AuditEventMessage>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [TestMethod]
    public async Task Update_ReturnsBadRequest_WhenServiceThrowsArgumentException()
    {
        Guid roomId = Guid.NewGuid();
        var request = new UpdateRoomRequest("", "Sala", 8, null);
        _roomService
            .Setup(s => s.GetByIdAsync(roomId, _companyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateRoomDto(roomId, _companyId, "Sala"));
        _roomService
            .Setup(s => s.UpdateAsync(roomId, _companyId, request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ArgumentException("El nombre del espacio es obligatorio."));

        ActionResult<RoomDto> result = await _controller.Update(roomId, request, CancellationToken.None);

        AssertStatus(result.Result, StatusCodes.Status400BadRequest, "El nombre del espacio es obligatorio.");
        _roomService.VerifyAll();
        _audit.Verify(a => a.PublishAsync(It.IsAny<AuditEventMessage>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [TestMethod]
    public async Task Delete_ReturnsNoContent_WhenCompanyClaimIsValid()
    {
        Guid roomId = Guid.NewGuid();
        _roomService
            .Setup(s => s.GetByIdAsync(roomId, _companyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateRoomDto(roomId, _companyId, "Sala"));
        _roomService
            .Setup(s => s.DeleteAsync(roomId, _companyId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _audit
            .Setup(a => a.PublishAsync(It.IsAny<AuditEventMessage>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        IActionResult result = await _controller.Delete(roomId, CancellationToken.None);

        Assert.IsInstanceOfType(result, typeof(NoContentResult));
        _roomService.VerifyAll();
        _audit.VerifyAll();
    }

    [TestMethod]
    public async Task Delete_ReturnsNoContent_WhenPreviousRoomDoesNotExistButDeleteCompletes()
    {
        Guid roomId = Guid.NewGuid();
        _roomService
            .Setup(s => s.GetByIdAsync(roomId, _companyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((RoomDto?)null);
        _roomService
            .Setup(s => s.DeleteAsync(roomId, _companyId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _audit
            .Setup(a => a.PublishAsync(It.IsAny<AuditEventMessage>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        IActionResult result = await _controller.Delete(roomId, CancellationToken.None);

        Assert.IsInstanceOfType(result, typeof(NoContentResult));
        _roomService.VerifyAll();
        _audit.VerifyAll();
    }

    [TestMethod]
    public async Task Delete_ReturnsForbidden_WhenCompanyClaimIsMissing()
    {
        _controller = CreateControllerWithoutCompanyClaim();

        IActionResult result = await _controller.Delete(Guid.NewGuid(), CancellationToken.None);

        AssertForbidden(result);
        _roomService.Verify(s => s.DeleteAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        _audit.Verify(a => a.PublishAsync(It.IsAny<AuditEventMessage>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [TestMethod]
    public async Task GetTopRooms_ReturnsOk_WithBearerAuthentication()
    {
        IReadOnlyList<TopRoomDto> topRooms = [new TopRoomDto(Guid.NewGuid(), "Sala", 3, _companyId)];
        _controller.Request.Headers.Authorization = "Bearer token-123";
        _reservationService
            .Setup(s => s.GetTopRoomsAsync(
                It.Is<ReservationServiceAuthContext>(a => a.HeaderName == "Authorization" && a.Value == "Bearer token-123"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(topRooms);

        ActionResult<IReadOnlyList<TopRoomDto>> result = await _controller.GetTopRooms(CancellationToken.None);

        var ok = result.Result as OkObjectResult;
        Assert.IsNotNull(ok);
        Assert.AreEqual(StatusCodes.Status200OK, ok.StatusCode);
        Assert.AreEqual(topRooms, ok.Value);
        _reservationService.VerifyAll();
    }

    [TestMethod]
    public async Task GetTopRooms_ReturnsOk_WithApiKeyAuthentication()
    {
        IReadOnlyList<TopRoomDto> topRooms = [new TopRoomDto(Guid.NewGuid(), "Sala", 3, _companyId)];
        _controller.Request.Headers["API_KEY"] = "api-key-123";
        _reservationService
            .Setup(s => s.GetTopRoomsAsync(
                It.Is<ReservationServiceAuthContext>(a => a.HeaderName == "API_KEY" && a.Value == "api-key-123"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(topRooms);

        ActionResult<IReadOnlyList<TopRoomDto>> result = await _controller.GetTopRooms(CancellationToken.None);

        var ok = result.Result as OkObjectResult;
        Assert.IsNotNull(ok);
        Assert.AreEqual(topRooms, ok.Value);
        _reservationService.VerifyAll();
    }

    [TestMethod]
    public async Task GetTopRooms_ReturnsForbidden_WhenCompanyClaimIsMissing()
    {
        _controller = CreateControllerWithoutCompanyClaim();

        ActionResult<IReadOnlyList<TopRoomDto>> result = await _controller.GetTopRooms(CancellationToken.None);

        AssertForbidden(result.Result);
        _reservationService.Verify(s => s.GetTopRoomsAsync(It.IsAny<ReservationServiceAuthContext>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [TestMethod]
    public async Task GetTopRooms_ThrowsUnauthorizedAccessException_WhenNoCredentialsWereProvided()
    {
        try
        {
            await _controller.GetTopRooms(CancellationToken.None);
            Assert.Fail("Expected an UnauthorizedAccessException to be thrown.");
        }
        catch (UnauthorizedAccessException ex)
        {
            Assert.AreEqual("No credentials were provided for reservation service forwarding.", ex.Message);
        }
    }

    [TestMethod]
    public async Task GetTopRooms_PropagatesException_WhenReservationClientFails()
    {
        _controller.Request.Headers.Authorization = "Bearer token-123";
        _reservationService
            .Setup(s => s.GetTopRoomsAsync(It.IsAny<ReservationServiceAuthContext>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Reservation service failed."));

        try
        {
            await _controller.GetTopRooms(CancellationToken.None);
            Assert.Fail("Expected an InvalidOperationException to be thrown.");
        }
        catch (InvalidOperationException ex)
        {
            Assert.AreEqual("Reservation service failed.", ex.Message);
        }

        _reservationService.VerifyAll();
    }

    private RoomsController CreateControllerWithCompanyClaim(Guid companyId) =>
        CreateControllerWithCompanyClaim(companyId.ToString());

    private RoomsController CreateControllerWithCompanyClaim(string companyClaimValue)
    {
        Claim[] claims =
        [
            new Claim("CompanyId", companyClaimValue),
            new Claim(ClaimTypes.NameIdentifier, _userId.ToString()),
            new Claim(ClaimTypes.Email, "member@workly.test")
        ];
        return CreateController(claims);
    }

    private RoomsController CreateControllerWithoutCompanyClaim() =>
        CreateController([new Claim(ClaimTypes.NameIdentifier, _userId.ToString())]);

    private RoomsController CreateController(IEnumerable<Claim> claims)
    {
        var controller = new RoomsController(_roomService.Object, _reservationService.Object, _audit.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test")),
                    TraceIdentifier = "trace-test"
                }
            }
        };

        return controller;
    }

    private static RoomDto CreateRoomDto(Guid id, Guid companyId, string name, string? equipment = null) =>
        new(id, companyId, name, "Sala", "Sala", 8, equipment);

    private static void AssertForbidden(IActionResult? result)
    {
        AssertStatus(result, StatusCodes.Status403Forbidden, "Forbidden. You do not have permission to access this resource.");
    }

    private static void AssertStatus(IActionResult? result, int expectedStatusCode, string expectedMessage)
    {
        var objectResult = result as ObjectResult;
        Assert.IsNotNull(objectResult);
        Assert.AreEqual(expectedStatusCode, objectResult.StatusCode);
        var error = objectResult.Value as ApiErrorResponse;
        Assert.IsNotNull(error);
        Assert.AreEqual(expectedStatusCode, error.StatusCode);
        Assert.AreEqual(expectedMessage, error.Message);
    }
}
