using BusinessLogic.Configuration;
using IBusinessLogic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace BusinessLogic;

public sealed class PasswordHasherService : IPasswordHasherService
{
    private readonly int _workFactor;

    public PasswordHasherService(
        IOptions<PasswordHashingSettings> options,
        IConfiguration configuration)
    {
        int configuredWorkFactor = options.Value.WorkFactor;

        string? envOverride = configuration["PASSWORD_HASH_WORK_FACTOR"];
        if (int.TryParse(envOverride, out int parsedEnvWorkFactor))
        {
            configuredWorkFactor = parsedEnvWorkFactor;
        }

        _workFactor = Math.Clamp(configuredWorkFactor, 4, 31);
    }

    public string HashPassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            throw new ArgumentException("Password cannot be empty.", nameof(password));
        }

        return BCrypt.Net.BCrypt.HashPassword(password, _workFactor);
    }

    public bool VerifyPassword(string password, string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(passwordHash))
        {
            return false;
        }

        return BCrypt.Net.BCrypt.Verify(password, passwordHash);
    }
}
