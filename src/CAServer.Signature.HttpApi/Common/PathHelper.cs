using System;
using System.IO;

namespace SignatureServer.Common;

public class PathHelper
{
    
    public static string ResolvePath(string path)
    {
        if (!path.StartsWith("~"))
        {
            return Path.IsPathRooted(path) ? path : Path.Combine(Directory.GetCurrentDirectory(), path);
        }

        // Home path
        var homeDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        path = path.Remove(0, 1).TrimStart(Path.DirectorySeparatorChar);
        return Path.Combine(homeDirectory, path);

    }
    
}