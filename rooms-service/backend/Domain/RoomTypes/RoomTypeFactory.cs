namespace Domain.RoomTypes;

/// <summary>
/// Resolves <see cref="RoomType"/> from a stable key and lists all built-in types.
/// To support new types over time, add a subclass of <see cref="RoomType"/> and
/// register it here (no CRUD for types in this iteration).
/// </summary>
public static class RoomTypeFactory
{
    private static readonly IReadOnlyDictionary<string, RoomType> ByKey =
        new Dictionary<string, RoomType>(StringComparer.OrdinalIgnoreCase)
        {
            [SalaRoomType.Instance.Key] = SalaRoomType.Instance,
            [EscritorioRoomType.Instance.Key] = EscritorioRoomType.Instance,
            [AuditorioRoomType.Instance.Key] = AuditorioRoomType.Instance,
        };

    private static readonly IReadOnlyList<RoomType> AllCached = ByKey.Values.ToList().AsReadOnly();

    public static IReadOnlyList<RoomType> All => AllCached;

    public static bool TryGet(string key, out RoomType? type)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            type = null;
            return false;
        }

        return ByKey.TryGetValue(key.Trim(), out type);
    }

    public static RoomType GetRequired(string key)
    {
        if (!TryGet(key, out var type) || type is null)
        {
            throw new ArgumentException(
                $"Tipo de espacio no reconocido: '{key}'. Use uno de: {string.Join(", ", ByKey.Keys)}.",
                nameof(key));
        }

        return type;
    }
}
