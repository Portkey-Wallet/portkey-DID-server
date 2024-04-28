using System;
using System.IO;

namespace CAServer.Commons;

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
        
    public static bool IsPathMatch(string requestPath, string pattern)
    {
        var requestSegments = requestPath.Trim('/').Split('/');
        var patternSegments = pattern.Trim('/').Split('/');
        int reqSegIdx = 0, patSegIdx = 0;

        while (reqSegIdx < requestSegments.Length && patSegIdx < patternSegments.Length)
        {
            switch (patternSegments[patSegIdx])
            {
                case "**":
                    if (++patSegIdx == patternSegments.Length) return true; // "**" at end matches rest of the path
                    while (reqSegIdx < requestSegments.Length && !requestSegments[reqSegIdx].Equals(patternSegments[patSegIdx], StringComparison.OrdinalIgnoreCase))
                        reqSegIdx++;
                    break;
                case "*":
                    reqSegIdx++; // Skip one segment
                    break;
                default:
                    if (!requestSegments[reqSegIdx++].Equals(patternSegments[patSegIdx], StringComparison.OrdinalIgnoreCase))
                        return false;
                    break;
            }
            patSegIdx++;
        }

        return reqSegIdx == requestSegments.Length && patSegIdx == patternSegments.Length;
    }
    
}