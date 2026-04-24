using DanmakuDownloader.Web.Models.Danmaku;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace DanmakuDownloader.Web.Services;

public class DanmakuFilterService
{
    private readonly Lock _lock = new();

    // 原始规则列表
    private List<DanmakuFilter> _rules = [];

    // 缓存解析后的规则，极大地提高过滤性能
    private List<string> _keywords  = [];
    private List<Regex>  _regexList = [];

    public DanmakuFilterService()
    {
        LoadFromDisk();
    }

    private void LoadFromDisk()
    {
        if (!File.Exists(StaticConfig.JsonRulePath))
        {
            _rules = [];
            return;
        }

        try
        {
            var json = File.ReadAllText(StaticConfig.JsonRulePath);
            _rules = JsonSerializer.Deserialize<List<DanmakuFilter>>(json) ?? [];
        }
        catch
        {
            _rules = [];
        }

        RefreshCache();
    }

    public void UpdateRules(List<DanmakuFilter> newRules)
    {
        lock (_lock)
        {
            _rules = newRules;
            RefreshCache();
        }
    }

    public void InsertRule(DanmakuFilter rule)
    {
        lock (_lock)
        {
            if (_rules.Contains(rule)) return;
            _rules.Add(rule);
            RefreshCache();
        }
    }

    public void InsertRules(List<DanmakuFilter> rules)
    {
        lock (_lock)
        {
            _rules.AddRange(rules.Where(rule => !_rules.Contains(rule)));
            RefreshCache();
        }
    }

    public List<DanmakuFilter> GetRules()
    {
        lock (_lock)
        {
            return _rules.ToList();
        }
    }

    public void SaveToDisk()
    {
        lock (_lock)
        {
            var json = JsonSerializer.Serialize(_rules, new JsonSerializerOptions { WriteIndented = true });

            var dir = Path.GetDirectoryName(StaticConfig.JsonRulePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            File.WriteAllText(StaticConfig.JsonRulePath, json);
        }
    }

    private void RefreshCache()
    {
        var newKeywords  = new List<string>();
        var newRegexList = new List<Regex>();

        foreach (var rule in _rules.Where(rule => rule.Opened))
        {
            switch (rule.Type)
            {
                case 0 :
                    newKeywords.Add(rule.Filter);
                    break;
                case 1 :
                    try
                    {
                        newRegexList.Add(new Regex(rule.Filter, RegexOptions.IgnoreCase));
                    }
                    catch
                    {
                        /* 忽略非法正则 */
                    }

                    break;
            }
        }

        _keywords  = newKeywords;
        _regexList = newRegexList;
    }

    /// <summary>
    /// 供其他 Service 调用的核心判断逻辑
    /// </summary>
    public bool IsFiltered(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return false;

        // 局部变量获取引用，保证并发下的线程安全
        var currentKeywords = _keywords;
        var currentRegex    = _regexList;

        return currentKeywords.Any(kw => text.Contains(kw, StringComparison.OrdinalIgnoreCase)) ||
               currentRegex.Any(regex => regex.IsMatch(text));
    }
}