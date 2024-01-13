using System;
using System.IO;
using Microsoft.Extensions.Configuration;
using SignatureServer.Common;
using SignatureServer.Dtos;

namespace SignatureServer.Command.Commands;

public class EncryptJsonGenerateCommand : ICommand
{
    private readonly IConfiguration _configuration;
    
    public EncryptJsonGenerateCommand(IConfiguration configuration)
    {
        _configuration = configuration;
    }
    
    public string Name()
    {
        return "Generate encrypt json file";
    }

    public void Run()
    {
        // The json file corresponding to key should not exist.
        var key = InputHelper.ReadText("Key: ");
        var path = $"/{key}.json";
        path = PathHelper.ResolvePath(_configuration.GetSection("KeyStore:Path").Get<string>()) + path;
        if (key.IsNullOrEmpty() || File.Exists(path))
        {
            Console.WriteLine($"File exists: {path}");
            Run();
            return;
        }
        
        var encryptDataBuilder = EncryptDataDto.Builder()
            .RandomNonce()
            .Key(key);

        // Enter the encryption method, enter ENTER directly,
        // and the Aes Gcm mode will be selected by default.
        var encryptTypeLabel = $"Encrypt type {string.Join("/", EncryptType.All)} (Default {EncryptType.Default}): ";
        bool encryptTypeSelected;
        do
        {
            encryptDataBuilder.EncryptType(InputHelper.ReadText(encryptTypeLabel), out encryptTypeSelected);
        } while (!encryptTypeSelected);

        // Input secret data
        encryptDataBuilder.Secret(InputHelper.ReadPassword("Secret [Hidden]: "));
        
        // Input password twice
        bool passwordMatch;
        do
        {
            encryptDataBuilder
                .Password(InputHelper.ReadPassword("Password [Hidden]: "))
                .RepeatPassword(InputHelper.ReadPassword("Repeat password [Hidden]: "), out passwordMatch);
            if (!passwordMatch) Console.WriteLine("Password not match");
        } while (!passwordMatch);
        
        // Input any Information
        var encryptDataDto = encryptDataBuilder
            .Information(InputHelper.ReadText("Information [optional]:"))
            .Build();

        var write = File.CreateText(path);
        write.Write(encryptDataDto.FormatJson());
        write.Flush();
        write.Close();
        
        Console.WriteLine("Encrypt json : " + encryptDataDto.FormatJson());
        Console.WriteLine($"Done! Encrypted file saved : {path}");
    }
}