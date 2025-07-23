using Banned.Logger;
using DanmakuDownloader.Models.Danmaku;
using Newtonsoft.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace DanmakuDownloader.Utils;

public class DanmakuUtils
{
    private static readonly Logger Logger = new(options =>
    {
        options.Name             = "DanmakuFilter";
        options.BaseDirectory    = "logs";
        options.LogFormat        = StaticConfig.LoggerFormat;
        options.WriteOnConsole   = true;
        options.MinimumLevel     = StaticConfig.LoggerLevel;
        options.MaxFileSize      = 5 * 1024 * 1024; // 5MB
        options.MaxRetainedFiles = 7;               // Keep logs for 7 days
        options.LogFormat        = "{timestamp:yyyy-MM-dd HH:mm:ss} {level} {name}: {message}";
    });

    private static readonly List<string> Keywords  = [];
    private static readonly List<Regex>  RegexList = [];

    static DanmakuUtils()
    {
        _ = LoadFilterRules();
    }

    public static async Task Filter(string filePath)
    {
        try
        {
            var changed = await FilterDanmakuFile(filePath);
            if (changed)
                await Logger.DebugAsync($"[✔] 处理完成: {filePath}");
        }
        catch (Exception ex)
        {
            await Logger.ErrorAsync($"[✗] 处理失败: {filePath}\n\t错误: {ex.Message}");
        }
    }

    private static async Task LoadFilterRules()
    {
        if (!File.Exists(StaticConfig.JsonRulePath))
        {
            await Logger.ErrorAsync("Filter json not exists.");
            return;
        }

        var json = await File.ReadAllTextAsync(StaticConfig.JsonRulePath);
        await Logger.DebugAsync("Read Filter Json.");
        var rules = JsonConvert.DeserializeObject<List<DanmakuFilter>>(json) ?? [];
        await Logger.DebugAsync($"Convert string to rule list, count: {rules.Count}");


        foreach (var rule in rules.Where(rule => rule.Opened))
        {
            switch (rule.Type)
            {
                case 0 :
                    Keywords.Add(rule.Filter);
                    break;
                case 1 :
                    try
                    {
                        RegexList.Add(new Regex(rule.Filter, RegexOptions.IgnoreCase));
                    }
                    catch (Exception ex)
                    {
                        await Logger.ErrorAsync($"[!] 无效正则: {rule.Filter}, 错误: {ex.Message}");
                    }

                    break;
            }
        }

        await Logger.DebugAsync($"Read finish, keywords count: {Keywords.Count}, Regex List count: {RegexList.Count}");
    }

    private static async Task<bool> FilterDanmakuFile(string filePath)
    {
        var doc = XDocument.Load(filePath);
        await Logger.DebugAsync("Read xml file");
        var root = doc.Root;

        if (root == null)
        {
            await Logger.ErrorAsync("Xml convert failed.");
            return false;
        }

        var originalNodes = root.Elements("d").ToList();
        var keptNodes = originalNodes
                       .Where(e => !IsFiltered(e.Value))
                       .ToList();

        if (originalNodes.Count == keptNodes.Count)
        {
            await Logger.DebugAsync("Filtered Danmaku not decrease");
            return false;
        }

        root.ReplaceNodes(keptNodes);

        doc.Save(filePath);
        return true;
    }

    private static bool IsFiltered(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return false;

        return Keywords.Any(kw => text.Contains(kw, StringComparison.OrdinalIgnoreCase)) ||
               RegexList.Any(regex => regex.IsMatch(text));
    }
}