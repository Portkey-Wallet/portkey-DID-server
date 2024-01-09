using System;
using System.IO;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using SignatureServer.Common;
using SignatureServer.Dtos;

namespace SignatureServer.Command.Commands;

public class EncryptJsonVerifyCommand : ICommand
{
    private readonly IConfiguration _configuration;
    
    public EncryptJsonVerifyCommand(IConfiguration configuration)
    {
        _configuration = configuration;
    }
    
    public string Name()
    {
        return "Verify encrypt json file";
    }

    public void Run()
    {
        try
        {
            var key = InputHelper.ReadText("Key: ");
            var path = $"/{key}.json";
            path = PathHelper.ResolvePath(_configuration.GetSection("KeyStore:Path").Get<string>()) + path;
            var json = File.ReadAllText(path);
            var encryptData = JsonConvert.DeserializeObject<EncryptDataDto>(json);
            encryptData.Decrypt(InputHelper.ReadPassword("Password: "));
            Console.WriteLine("Encrypt json verified!");
        }
        catch (Exception e)
        {
            Console.WriteLine("Verify encrypt json failed: " + e.Message);
        }

        

    }
    
    
}