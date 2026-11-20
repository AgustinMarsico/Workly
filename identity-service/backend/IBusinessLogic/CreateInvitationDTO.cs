namespace IBusinessLogic.DTOs;

public record CreateInvitationDTO
{
    public required string UserEmail { get; init; }
    public required Guid CompanyId { get; init; }
    public required Guid CreatedById { get; init; }
    public required Guid AreaId { get; init; }
}