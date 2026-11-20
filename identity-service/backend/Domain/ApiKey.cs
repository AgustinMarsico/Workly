namespace Domain;

public class ApiKey
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string KeyHash { get; set; } = null!;

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}