using System.Text.Json;

namespace DanmakuDownloader.Models;

public static class JsonSetting
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);
}