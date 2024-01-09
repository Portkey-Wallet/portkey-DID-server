using System;
using System.IO;
using Microsoft.Extensions.Configuration;
using SignatureServer.Common;
using SignatureServer.Dtos;

namespace SignatureServer.Command.Commands;

public class EncryptJsonGenerateCommand : ICommand
{
    private IConfiguration _configuration;
    
    public EncryptJsonGenerateCommand(IConfiguration configuration)
    {
        this._configuration = configuration;
    }
    
    public string Name()
    {
        return "Generate encrypt json file";
    }

    public void Run()
    {
        var key = InputHelper.ReadText("Key: ");
        var path = $"/{key}.json";
        path = PathHelper.ResolvePath(_configuration.GetSection("KeyStore:Path").Get<string>()) + path;
        if (File.Exists(path))
        {
            Console.WriteLine($"File exists: {path}");
            Run();
            return;
        }

        var builder = EncryptDataDto.Builder()
            .Key(key)
            .Secret(InputHelper.ReadPassword("Secret: "));
        
        while (!builder.PasswordMatch)
        {
            builder.Password(InputHelper.ReadPassword("Password: "))
                .RepeatPassword(InputHelper.ReadPassword("Repeat password: "));
            if (!builder.PasswordMatch)
                Console.Write("Password not match");
        }
        var encryptDataDto = builder
            .Information(InputHelper.ReadText("Information:"))
            .RandomSalt().Build();

        var write = File.CreateText(path);
        write.Write(encryptDataDto.FormatJson());
        write.Flush();
        write.Close();
        
        Console.WriteLine("json: " + encryptDataDto.FormatJson());
        Console.WriteLine($"Done! Encrypted file saved at {path}");
        
    }
}