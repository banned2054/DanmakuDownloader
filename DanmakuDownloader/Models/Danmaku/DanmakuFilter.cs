namespace DanmakuDownloader.Models.Danmaku;

public class DanmakuFilter : IEquatable<DanmakuFilter>
{
    public bool Equals(DanmakuFilter? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;

        return Type == other.Type && Filter == other.Filter;
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as DanmakuFilter);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Type, Filter);
    }

    public static bool operator ==(DanmakuFilter? left, DanmakuFilter? right)
    {
        return left?.Equals(right) ?? right is null;
    }

    public static bool operator !=(DanmakuFilter? left, DanmakuFilter? right)
    {
        return !(left == right);
    }

    public int    Type   { get; set; } // 0: keyword, 1: regex
    public string Filter { get; set; } = string.Empty;
    public bool   Opened { get; set; }
}