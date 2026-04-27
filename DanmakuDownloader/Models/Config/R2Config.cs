namespace DanmakuDownloader.Models.Config;

public class R2Config
{
    public override bool Equals(object? obj)
    {
        return obj switch
        {
            R2Config config => Access   == config.Access   &&
                               Secret   == config.Secret   &&
                               Endpoint == config.Endpoint &&
                               Bucket   == config.Bucket,
            _ => false
        };
    }

    public static bool operator ==(R2Config? left, R2Config? right)
    {
        return left?.Equals(right) ?? ReferenceEquals(right, null);
    }

    public static bool operator !=(R2Config? left, R2Config? right)
    {
        return !(left == right);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Access, Secret, Endpoint, Bucket);
    }

    public string? Access   { get; set; }
    public string? Secret   { get; set; }
    public string? Endpoint { get; set; }
    public string? Bucket   { get; set; }
}