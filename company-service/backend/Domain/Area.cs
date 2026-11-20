namespace Domain;

public class Area
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public required string Name { get; set; }
    public required string Description { get; set; }
}
