using System;
using Microsoft.Extensions.Configuration;

namespace SignatureServer.Command.Commands;

public class ExitCommand: ICommand
{
    public ExitCommand(IConfiguration configuration)
    {
    }

    public string Name()
    {
        return "Exit";
    }

    public void Run()
    {
        Environment.Exit(0);
    }
}