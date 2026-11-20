using System.Security.Claims;
using System.Text.Json;
using AuditService.Api.Contracts;
using AuditService.Api.Controllers;
using AuditService.Api.Data;
using AuditService.Api.Domain;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace AuditService.Tests;

[TestClass]
public sealed class AuditRecordsControllerTests
{
    private Mock<IAuditRecordRepository>? _repository;
    private AuditRecordsController _controller = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _repository = new Mock<IAuditRecordRepository>();
        _controller = new AuditRecordsController(_repository.Object);
        SetUser();
    }

    [TestMethod]
    public async Task Get_ReturnsOkAndUsesRequestFilters_WhenUserHasNoCompanyClaim()
    {
        Guid companyId = Guid.NewGuid();
        Guid userId = Guid.NewGuid();
        DateTimeOffset from = new(2026, 6, 1, 0, 0, 0, TimeSpan.Zero);
        DateTimeOffset to = new(2026, 6, 30, 23, 59, 0, TimeSpan.Zero);
        var request = new AuditRecordsQueryRequest
        {
            From = from,
            To = to,
            UserId = userId,
            ActionType = nameof(AuditActionType.ReservationCreated),
            CompanyId = companyId,
            Page = 2,
            PageSize = 10,
        };
        var record = CreateAuditRecord(companyId: companyId, userId: userId);
        _repository!
            .Setup(r => r.QueryAsync(
                It.Is<AuditRecordQuery>(q =>
                    q.From == from
                    && q.To == to
                    && q.UserId == userId
                    && q.ActionType == request.ActionType
                    && q.CompanyId == companyId
                    && q.Page == request.Page
                    && q.PageSize == request.PageSize),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreatePagedResult(record, request.Page, request.PageSize, 1));

        var result = await _controller.Get(request, CancellationToken.None);

        var ok = result.Result as OkObjectResult;
        Assert.IsNotNull(ok);
        var response = ok!.Value as PagedResult<AuditRecordResponse>;
        Assert.IsNotNull(response);
        Assert.AreEqual(2, response!.Page);
        Assert.AreEqual(10, response.PageSize);
        Assert.AreEqual(1, response.TotalCount);
        Assert.AreEqual(1, response.Items.Count);
        Assert.AreEqual(record.Id, response.Items[0].Id);
        Assert.AreEqual(record.ActionType, response.Items[0].ActionType);
        Assert.AreEqual(record.CompanyId, response.Items[0].CompanyId);

        _repository.Verify(r => r.QueryAsync(It.IsAny<AuditRecordQuery>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task Get_ReturnsOkAndUsesClaimCompanyId_WhenUserHasCompanyClaim()
    {
        Guid claimCompanyId = Guid.NewGuid();
        var request = new AuditRecordsQueryRequest
        {
            ActionType = nameof(AuditActionType.ApiKeyCreated),
            Page = 1,
            PageSize = 25,
        };
        SetUser(claimCompanyId);
        var record = CreateAuditRecord(companyId: claimCompanyId);
        _repository!
            .Setup(r => r.QueryAsync(
                It.Is<AuditRecordQuery>(q =>
                    q.CompanyId == claimCompanyId
                    && q.ActionType == request.ActionType
                    && q.Page == request.Page
                    && q.PageSize == request.PageSize),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreatePagedResult(record, request.Page, request.PageSize, 1));

        var result = await _controller.Get(request, CancellationToken.None);

        var ok = result.Result as OkObjectResult;
        Assert.IsNotNull(ok);
        var response = ok!.Value as PagedResult<AuditRecordResponse>;
        Assert.IsNotNull(response);
        Assert.AreEqual(claimCompanyId, response!.Items[0].CompanyId);

        _repository.Verify(r => r.QueryAsync(It.IsAny<AuditRecordQuery>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task Get_ReturnsForbidAndDoesNotQueryRepository_WhenUserRequestsDifferentCompany()
    {
        Guid claimCompanyId = Guid.NewGuid();
        Guid requestedCompanyId = Guid.NewGuid();
        SetUser(claimCompanyId);
        var request = new AuditRecordsQueryRequest
        {
            CompanyId = requestedCompanyId,
            Page = 1,
            PageSize = 50,
        };

        var result = await _controller.Get(request, CancellationToken.None);

        Assert.IsInstanceOfType(result.Result, typeof(ForbidResult));
        _repository!.Verify(r => r.QueryAsync(It.IsAny<AuditRecordQuery>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [TestMethod]
    public async Task Get_ReturnsOkAndMapsAuditRecordFields()
    {
        Guid companyId = Guid.NewGuid();
        Guid userId = Guid.NewGuid();
        var record = CreateAuditRecord(
            companyId: companyId,
            userId: userId,
            actionType: nameof(AuditActionType.PhysicalSpaceCreated),
            occurredAt: new DateTimeOffset(2026, 6, 14, 9, 15, 0, TimeSpan.Zero),
            userEmail: "admin@workly.test",
            entityType: "Room",
            entityId: "room-1",
            detailsJson: """{"name":"Sala A"}""",
            oldValuesJson: """{"name":"Sala Vieja"}""",
            newValuesJson: """{"name":"Sala A"}""",
            correlationId: "corr-1",
            createdAt: new DateTimeOffset(2026, 6, 15, 10, 30, 0, TimeSpan.Zero));
        _repository!
            .Setup(r => r.QueryAsync(It.IsAny<AuditRecordQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreatePagedResult(record, 1, 50, 1));

        var result = await _controller.Get(new AuditRecordsQueryRequest(), CancellationToken.None);

        var ok = result.Result as OkObjectResult;
        Assert.IsNotNull(ok);
        var response = ok!.Value as PagedResult<AuditRecordResponse>;
        Assert.IsNotNull(response);
        AuditRecordResponse item = response!.Items[0];
        Assert.AreEqual(record.Id, item.Id);
        Assert.AreEqual(record.EventId, item.EventId);
        Assert.AreEqual(record.OccurredAt, item.OccurredAt);
        Assert.AreEqual(record.ActionType, item.ActionType);
        Assert.AreEqual(record.UserEmail, item.UserEmail);
        Assert.AreEqual(record.EntityType, item.EntityType);
        Assert.AreEqual(record.EntityId, item.EntityId);
        Assert.AreEqual(record.CompanyId, item.CompanyId);
        Assert.AreEqual(record.UserId, item.UserId);
        Assert.AreEqual(record.DetailsJson, item.DetailsJson);
        Assert.AreEqual(record.OldValuesJson, item.OldValuesJson);
        Assert.AreEqual(record.NewValuesJson, item.NewValuesJson);
        Assert.AreEqual(record.CorrelationId, item.CorrelationId);
        Assert.AreEqual(record.CreatedAt, item.CreatedAt);

        _repository.Verify(r => r.QueryAsync(It.IsAny<AuditRecordQuery>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    private void SetUser(Guid? companyId = null)
    {
        var claims = new List<Claim>();
        if (companyId.HasValue)
        {
            claims.Add(new Claim("CompanyId", companyId.Value.ToString()));
        }

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(claims, authenticationType: "Test")),
            },
        };
    }

    private static PagedResult<AuditRecord> CreatePagedResult(
        AuditRecord record,
        int page,
        int pageSize,
        int totalCount)
    {
        return new PagedResult<AuditRecord>([record], page, pageSize, totalCount);
    }

    private static AuditRecord CreateAuditRecord(
        Guid? companyId = null,
        Guid? userId = null,
        string actionType = nameof(AuditActionType.ReservationCreated),
        DateTimeOffset? occurredAt = null,
        string? userEmail = "admin@workly.test",
        string entityType = "Reservation",
        string entityId = "reservation-1",
        string detailsJson = """{"source":"test"}""",
        string? oldValuesJson = null,
        string? newValuesJson = null,
        string? correlationId = "corr-1",
        DateTimeOffset? createdAt = null)
    {
        return AuditRecord.FromEvent(new AuditEvent(
            Guid.NewGuid(),
            occurredAt ?? new DateTimeOffset(2026, 6, 14, 12, 0, 0, TimeSpan.Zero),
            actionType,
            userId ?? Guid.NewGuid(),
            userEmail,
            companyId ?? Guid.NewGuid(),
            entityType,
            entityId,
            JsonDocument.Parse(detailsJson).RootElement,
            oldValuesJson is null ? null : JsonDocument.Parse(oldValuesJson).RootElement,
            newValuesJson is null ? null : JsonDocument.Parse(newValuesJson).RootElement,
            correlationId), createdAt);
    }
}
