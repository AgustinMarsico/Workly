namespace Domain.RoomTypes;

/// <summary>
/// Base type for physical room categories. New kinds can be added by subclassing
/// and registering them in <see cref="RoomTypeFactory"/>.
/// </summary>
public abstract class RoomType : IEquatable<RoomType>
{
    public abstract string Key { get; }

    public abstract string DisplayName { get; }

    public bool Equals(RoomType? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return string.Equals(Key, other.Key, StringComparison.Ordinal);
    }

    public override bool Equals(object? obj) => obj is RoomType other && Equals(other);

    public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Key);

    public static bool operator ==(RoomType? left, RoomType? right) =>
        left is null ? right is null : left.Equals(right);

    public static bool operator !=(RoomType? left, RoomType? right) => !(left == right);
}
