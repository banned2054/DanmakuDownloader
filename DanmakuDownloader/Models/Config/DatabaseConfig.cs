namespace DanmakuDownloader.Models.Config;

public class DatabaseConfig
{
    public override bool Equals(object? obj)
    {
        return obj switch
        {
            DatabaseConfig config => Host     == config.Host     && Port == config.Port &&
                                     UserName == config.UserName &&
                                     Password == config.Password && Table == config.Table,
            _ => false
        };
    }

    public static bool operator ==(DatabaseConfig? left, DatabaseConfig? right)
    {
        return left?.Equals(right) ?? ReferenceEquals(right, null);
    }

    public static bool operator !=(DatabaseConfig? left, DatabaseConfig? right)
    {
        return !(left == right);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Host, Port, UserName, Password, Table);
    }

    public string? Host     { get; set; }
    public int?    Port     { get; set; }
    public string? UserName { get; set; }
    public string? Password { get; set; }
    public string? Table    { get; set; }
}
