using System.Security.Cryptography;
using System.Text;
using CustomExceptions;
using Domain;
using IBusinessLogic;
using IBusinessLogic.ApiKeys;
using IDataAccess;
using Microsoft.Extensions.Configuration;

namespace BusinessLogic;

public sealed class ApiKeyService(
    IApiKeyRepository apiKeys,
    IUserRepository users,
    IConfiguration configuration) : IApiKeyService
{
    public async Task<CreatedApiKeyResult> CreateAsync(
        Guid userId,
        Guid companyId,
        string name,
        CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("UserId cannot be empty.", nameof(userId));
        }

        if (companyId == Guid.Empty)
        {
            throw new ArgumentException("CompanyId cannot be empty.", nameof(companyId));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Name cannot be empty.", nameof(name));
        }

        string normalizedName = name.Trim();
        if (normalizedName.Length > 200)
        {
            throw new ArgumentException("Name cannot exceed 200 characters.", nameof(name));
        }

        User user = await users.GetByIdAsync(userId, cancellationToken).ConfigureAwait(false);
        EnsureMemberBelongsToCompany(user, companyId);

        if (await apiKeys.ExistsActiveByNameForCompanyAsync(companyId, normalizedName, cancellationToken)
                .ConfigureAwait(false))
        {
            throw new ArgumentException(
                "Ya existe una clave de API activa con el mismo nombre para esta empresa.",
                nameof(name));
        }

        string apiKeyValue = GenerateApiKeyValue();
        string hash = ComputeKeyHash(apiKeyValue);

        var entity = new ApiKey
        {
            Id = Guid.NewGuid(),
            Name = normalizedName,
            KeyHash = hash,
            UserId = userId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        ApiKey created = await apiKeys.CreateAsync(entity, cancellationToken).ConfigureAwait(false);

        return new CreatedApiKeyResult(
            created.Id,
            created.Name,
            apiKeyValue,
            created.CreatedAt);
    }

    public async Task<bool> RevokeAsync(
        Guid userId,
        Guid companyId,
        Guid apiKeyId,
        CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("UserId cannot be empty.", nameof(userId));
        }

        if (companyId == Guid.Empty)
        {
            throw new ArgumentException("CompanyId cannot be empty.", nameof(companyId));
        }

        if (apiKeyId == Guid.Empty)
        {
            throw new ArgumentException("ApiKeyId cannot be empty.", nameof(apiKeyId));
        }

        User user = await users.GetByIdAsync(userId, cancellationToken).ConfigureAwait(false);
        EnsureMemberBelongsToCompany(user, companyId);

        return await apiKeys.RevokeAsync(apiKeyId, userId, cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<ApiKeyListItem>> ListAsync(
        Guid userId,
        Guid companyId,
        CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("UserId cannot be empty.", nameof(userId));
        }

        if (companyId == Guid.Empty)
        {
            throw new ArgumentException("CompanyId cannot be empty.", nameof(companyId));
        }

        User user = await users.GetByIdAsync(userId, cancellationToken).ConfigureAwait(false);
        EnsureMemberBelongsToCompany(user, companyId);

        IReadOnlyList<(Guid Id, string Name)> rows =
            await apiKeys.ListIdAndNameByCompanyAsync(companyId, cancellationToken).ConfigureAwait(false);

        return rows.Select(r => new ApiKeyListItem(r.Id, r.Name)).ToList();
    }

    public async Task<ApiKeyValidationResult> ValidateAsync(
        string apiKeyValue,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(apiKeyValue))
        {
            return new ApiKeyValidationResult(false);
        }

        string hash = ComputeKeyHash(apiKeyValue.Trim());
        ApiKey? apiKey = await apiKeys.GetActiveByHashAsync(hash, cancellationToken).ConfigureAwait(false);
        if (apiKey is null)
        {
            return new ApiKeyValidationResult(false);
        }

        return new ApiKeyValidationResult(
            true,
            apiKey.UserId,
            apiKey.User.CompanyId,
            apiKey.User.Role,
            apiKey.IsActive);
    }

    private static void EnsureMemberBelongsToCompany(User user, Guid companyId)
    {
        if (!string.Equals(user.Role, UserRoles.Member, StringComparison.OrdinalIgnoreCase))
        {
            throw new ForbiddenException(
                "Solo los usuarios con rol miembro pueden administrar claves de API.");
        }

        if (user.CompanyId is not Guid userCompanyId || userCompanyId != companyId)
        {
            throw new ForbiddenException("El usuario no pertenece a la empresa.");
        }
    }

    private string GenerateApiKeyValue()
    {
        byte[] bytes = RandomNumberGenerator.GetBytes(32);
        return Base64UrlEncode(bytes);
    }

    private string ComputeKeyHash(string apiKeyValue)
    {
        string secret = configuration["API_KEY_SECRET"]
            ?? configuration["SUPER_SECRET_KEY"]
            ?? throw new InvalidOperationException("Missing API key hashing secret. Configure API_KEY_SECRET or SUPER_SECRET_KEY.");

        byte[] key = Encoding.UTF8.GetBytes(secret);
        byte[] data = Encoding.UTF8.GetBytes(apiKeyValue);

        byte[] hashBytes = HMACSHA256.HashData(key, data);
        return ConvertToLowerHex(hashBytes);
    }

    private static string Base64UrlEncode(byte[] data)
    {
        string base64 = Convert.ToBase64String(data);
        return base64.TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    private static string ConvertToLowerHex(byte[] bytes)
    {
        var sb = new StringBuilder(bytes.Length * 2);
        foreach (byte b in bytes)
        {
            sb.Append(b.ToString("x2"));
        }

        return sb.ToString();
    }
}
