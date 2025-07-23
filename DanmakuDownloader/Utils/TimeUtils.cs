using System.Globalization;
using TimeZoneConverter;

namespace DanmakuDownloader.Utils;

public class TimeUtils
{
    /// <summary>
    /// 获取北京时间的现在的时间
    /// </summary>
    public static DateTimeOffset GetNow()
    {
        var tz = TZConvert.GetTimeZoneInfo("Asia/Shanghai");
        return TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, tz); // 正确保留 +08:00
    }

    public static DateTimeOffset? ParseString(string timeStr, string format = "yyyy-MM-dd HH:mm:ss")
    {
        if (string.IsNullOrEmpty(timeStr)) return null;
        try
        {
            var dt = DateTime.ParseExact(timeStr, format, CultureInfo.InvariantCulture, DateTimeStyles.None);
            return new DateTimeOffset(dt, TimeSpan.FromHours(8)); // 明确标记为北京时间
        }
        catch (Exception ex)
        {
            {
                Console.WriteLine($"Time format error, origin string: {timeStr}, error: {ex.Message}");
            }
        }

        return null;
    }
}