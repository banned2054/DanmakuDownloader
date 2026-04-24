using System.Text.Json;

namespace DanmakuDownloader.Web.Models;

public static class JsonSetting
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);
}