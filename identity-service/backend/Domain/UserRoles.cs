namespace Domain;

public static class UserRoles
{
    public const string Admin = "Admin";
    public const string Member = "Member";

    public static bool TryNormalize(string? input, out string normalized)
    {
        normalized = string.Empty;
        if (string.IsNullOrWhiteSpace(input))
        {
            return false;
        }

        string role = input.Trim();

        if (role.Equals(Admin, StringComparison.OrdinalIgnoreCase) || role.Equals("administrador", StringComparison.OrdinalIgnoreCase))
        {
            normalized = Admin;
            return true;
        }

        if (role.Equals(Member, StringComparison.OrdinalIgnoreCase) || role.Equals("miembro", StringComparison.OrdinalIgnoreCase))
        {
            normalized = Member;
            return true;
        }

        return false;
    }
}
