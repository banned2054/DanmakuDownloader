namespace DanmakuDownloader.Models.Config;

public class JellyfinConfig
{
    public void Copy(JellyfinConfig config)
    {
        Url      = config.Url;
        UserName = config.UserName;
        Password = config.Password;
    }

    public override bool Equals(object? obj)
    {
        return obj switch
        {
            JellyfinConfig config => Url      == config.Url      &&
                                     UserName == config.UserName &&
                                     Password == config.Password,
            _ => false
        };
    }

    public static bool operator ==(JellyfinConfig? left, JellyfinConfig? right)
    {
        return left?.Equals(right) ?? ReferenceEquals(right, null);
    }

    public static bool operator !=(JellyfinConfig? left, JellyfinConfig? right)
    {
        return !(left == right);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Url, UserName, Password);
    }

    public string? Url      { get; set; }
    public string? UserName { get; set; }
    public string? Password { get; set; }
}