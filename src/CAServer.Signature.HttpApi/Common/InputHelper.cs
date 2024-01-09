using System;

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
    
}