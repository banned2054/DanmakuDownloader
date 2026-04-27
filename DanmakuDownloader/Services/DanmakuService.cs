using System.Xml.Linq;

namespace DanmakuDownloader.Services;

public class DanmakuService(DanmakuFilterService filterService)
{
    /// <summary>
    /// 读取本地弹幕文件，应用过滤规则并保存
    /// </summary>
    public bool FilterDanmakuFile(string filePath)
    {
        if (!File.Exists(filePath)) return false;

        try
        {
            var doc  = XDocument.Load(filePath);
            var root = doc.Root;

            if (root == null) return false;

            var originalNodes = root.Elements("d").ToList();

            var keptNodes = originalNodes
                           .Where(e => !filterService.IsFiltered(e.Value))
                           .ToList();

            if (originalNodes.Count == keptNodes.Count)
            {
                return true;
            }

            root.ReplaceNodes(keptNodes);

            doc.Save(filePath);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }
}