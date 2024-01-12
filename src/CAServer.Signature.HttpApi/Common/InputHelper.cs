using System;
using System.IO;
using Volo.Abp;

namespace SignatureServer.Common;

public static class InputHelper
{
    
    public static string ReadText(string label = "")
    {
        Console.Write(label);
        return Console.ReadLine() ?? "";
    }
    
    public static string ReadPassword(string label = "")
    {
        Console.Write(label);
        var pwd = "";
        while (true)
        {
            var key = Console.ReadKey(true);
            if (key.Key == ConsoleKey.Enter)
            {
                Console.WriteLine();
                break;
            }
            if (key.Key == ConsoleKey.Backspace && pwd.Length > 0)
            {
                pwd = pwd[..^1];
            }
            else if (key.Key != ConsoleKey.Backspace)
            {
                pwd += key.KeyChar;
            }
        }
        
        return pwd;
    }
    
    public static string ReadFile(string path)
    {
        if (!File.Exists(path))
        {
            throw new UserFriendlyException("File not exits " + path);
        }
        using var textReader = File.OpenText(path);
        return textReader.ReadToEnd();
    }

    
}