namespace BusinessLogic.Configuration;

public sealed class PasswordHashingSettings
{
    public const string SectionName = "PasswordHashing";

    public int WorkFactor { get; set; } = 11;
}
