namespace DanmakuDownloader.Web.Models.Config;

public class TimeConfig
{
    public static bool operator ==(TimeConfig? left, TimeConfig? right)
    {
        return left?.Equals(right) ?? ReferenceEquals(right, null);
    }

    public static bool operator !=(TimeConfig? left, TimeConfig? right)
    {
        return !(left == right);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(TimeZone, ColdUpdateCronExp, HotUpdateCronExp);
    }

    public string? TimeZone          { get; set; }
    public string? ColdUpdateCronExp { get; set; }
    public string? HotUpdateCronExp  { get; set; }
}