using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;

namespace AuditService.Api.Data;

public static partial class AuditDatabasePermissions
{
    public static async Task ApplyRestrictedUserPermissions(
        AuditDbContext db,
        IConfiguration configuration)
    {
        string? username = configuration["AUDIT_DB_APP_USERNAME"];
        string? password = configuration["AUDIT_DB_APP_PASSWORD"];

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            return;
        }

        if (!SqlIdentifierRegex().IsMatch(username))
        {
            throw new InvalidOperationException("AUDIT_DB_APP_USERNAME must contain only letters, numbers, and underscores.");
        }

        string quotedUser = $"[{username}]";
        string escapedPassword = password.Replace("'", "''", StringComparison.Ordinal);

        string sql = $"""
IF NOT EXISTS (SELECT 1 FROM sys.sql_logins WHERE name = N'{username}')
BEGIN
    CREATE LOGIN {quotedUser} WITH PASSWORD = N'{escapedPassword}';
END
ELSE
BEGIN
    ALTER LOGIN {quotedUser} WITH PASSWORD = N'{escapedPassword}';
END;

IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = N'{username}')
BEGIN
    CREATE USER {quotedUser} FOR LOGIN {quotedUser};
END
ELSE
BEGIN
    ALTER USER {quotedUser} WITH LOGIN = {quotedUser};
END;

GRANT SELECT, INSERT ON OBJECT::dbo.audit_records TO {quotedUser};
DENY UPDATE, DELETE ON OBJECT::dbo.audit_records TO {quotedUser};
""";

        await db.Database.ExecuteSqlRawAsync(sql);
    }

    [GeneratedRegex("^[A-Za-z_][A-Za-z0-9_]{0,127}$")]
    private static partial Regex SqlIdentifierRegex();
}
