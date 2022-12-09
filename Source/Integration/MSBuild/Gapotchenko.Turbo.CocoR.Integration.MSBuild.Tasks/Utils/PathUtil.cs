﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace Gapotchenko.Turbo.CocoR.Integration.MSBuild.Tasks.Utils;

static class PathUtil
{
    public static string GetRelativePath(string relativeTo, string path)
    {
#if NET
        return Path.GetRelativePath(relativeTo, path);
#else
        return GetRelativePathCore(relativeTo, path);
#endif
    }

#if !NET
    static string GetRelativePathCore(string relativeTo, string path)
    {
        path = Path.GetFullPath(path);
        relativeTo = Path.GetFullPath(relativeTo);

        var separators = new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar };
        IReadOnlyList<string> p1 = path.Split(separators);
        IReadOnlyList<string> p2 = relativeTo.Split(separators, StringSplitOptions.RemoveEmptyEntries);

        var sc = StringComparison;

        int i;
        int n = Math.Min(p1.Count, p2.Count);
        for (i = 0; i < n; i++)
            if (!string.Equals(p1[i], p2[i], sc))
                break;

        if (i == 0)
        {
            // Cannot make a relative path, for example if the path resides on another drive.
            return path;
        }

        p1 = p1.Skip(i).Take(p1.Count - i).ToList();

        if (p1.Count == 1 && p1[0].Length == 0)
            p1 = Array.Empty<string>();

        string relativePath = string.Join(
            new string(Path.DirectorySeparatorChar, 1),
            Enumerable.Repeat("..", p2.Count - i).Concat(p1));

        if (relativePath.Length == 0)
            relativePath = ".";

        return relativePath;
    }

    static StringComparison StringComparison =>
        IsCaseSensitive?
            StringComparison.Ordinal :
            StringComparison.OrdinalIgnoreCase;

    static bool IsCaseSensitive =>
        !(RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ||
        RuntimeInformation.IsOSPlatform(OSPlatform.OSX));
#endif
}
