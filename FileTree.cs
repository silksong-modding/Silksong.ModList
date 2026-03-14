using System;
using System.IO;
using System.Linq;
using System.Text;

namespace Silksong.Modlist;

public static class FileTree
{
    public static int MAX_DEPTH = 3;

    private static readonly Func<string, bool>[] _excludes =
    [
        s => s.EndsWith(".old"),
        s => s == "manifest.json",
        s => s == "icon.png",
        s => s == "README.md",
        s => s == "CHANGELOG.md",
        s => s == "LICENSE"
    ];
    
    public static StringBuilder RenderTree(string dir, StringBuilder? builder = null, string prefix = "", int depth = 0)
    {
        builder ??= new StringBuilder();
        
        if (depth >= MAX_DEPTH)
        {
            return builder;
        }
        
        var di = new DirectoryInfo(dir);
        var items = di.GetFileSystemInfos().OrderBy(f => f.Name).ToList();

        foreach (var item in items.Take(items.Count - 1))
        {
            RenderItem(item, builder, false, prefix, depth);
        }
        
        var last =  items.LastOrDefault();
        if (last != null)
        {
            RenderItem(last, builder, true, prefix, depth);
        }
        
        return builder;
    }

    private static bool DirContainsOnlyExcluded(DirectoryInfo dir)
    {
        return dir.GetDirectories().Length == 0 && dir.GetFiles().All(info => _excludes.Any(ex => ex.Invoke(info.Name)));
    }

    private static void RenderItem(FileSystemInfo item, StringBuilder builder, bool lastItem, string prefix = "", int depth = 0)
    {
        if (_excludes.Any(ex => ex.Invoke(item.Name.TrimEnd()))) return;
        if (item.IsDirectory() && DirContainsOnlyExcluded((DirectoryInfo)item)) return;
        
        var suffix = item.IsDirectory() ? "/" : null;
        var postPrefix = lastItem ? "└── " : "├── ";
        builder.AppendLine($"{prefix}{postPrefix}{item.Name}{suffix}");
        if (item.IsDirectory())
        {
            RenderTree(item.FullName, builder, prefix + (lastItem ? "    ": "│   "), depth + 1);
        }
    }
}

public static class FileSystemInfoExtensions
{
    public static bool IsDirectory(this FileSystemInfo listing)
    {
        return listing.Attributes.HasFlag(FileAttributes.Directory);
    }
}