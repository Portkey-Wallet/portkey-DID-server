using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using SignatureServer.Command.Commands;
using SignatureServer.Common;

namespace SignatureServer.Command;

public class CommandProvider
{
    private readonly List<ICommand> _commands = new();
    private readonly IConfiguration _configuration;

    public CommandProvider(IConfiguration configuration)
    {
        _configuration = configuration;
        _commands.Add(new ExitCommand(configuration));
        _commands.Add(new EncryptJsonGenerateCommand(configuration));
        _commands.Add(new EncryptJsonVerifyCommand(configuration));
    }

    public void PrintMenu()
    {
        Console.WriteLine();
        Console.WriteLine();
        Console.WriteLine("aaaaaaaaaaaaaaaaaaaaaaaaa  Command Menu  aaaaaaaaaaaaaaaaaaaaaaaaa");
        Console.WriteLine();
        for (var i = 0; i < _commands.Count; i++)
        {
            Console.WriteLine($"            {i}. {_commands[i].Name()} ");
        }
        Console.WriteLine();
        Console.WriteLine("aaaaaaaaaaaaaaaaaaaaaaaaa Select command aaaaaaaaaaaaaaaaaaaaaaaaa");
        Console.WriteLine("Keystore path: " + PathHelper.ResolvePath(_configuration.GetSection("KeyStore:Path").Get<string>()));
    }


    public void Start()
    {
        while (true)
        {
            PrintMenu();
            int select;
            while (!int.TryParse(InputHelper.ReadText("Select: "), out select) || select >= _commands.Count)
            {
            }

            Console.WriteLine();
            Console.WriteLine($">>> [{_commands[select].Name()}] selected");
            _commands[select].Run();
            
            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine("Press ENTER to back!");
            while (ConsoleKey.Enter != Console.ReadKey(true).Key)
            {
                // do nothing
            }
            Console.Clear();
        }
    }

}