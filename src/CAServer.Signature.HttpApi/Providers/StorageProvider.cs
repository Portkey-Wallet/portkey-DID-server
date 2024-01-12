using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using SignatureServer.Common;
using SignatureServer.Dtos;
using SignatureServer.Options;
using SignatureServer.Providers.ExecuteStrategy;
using Volo.Abp;
using Volo.Abp.DependencyInjection;

namespace SignatureServer.Providers;

public class StorageProvider : ISingletonDependency
{
    private readonly ILogger<StorageProvider> _logger;
    private readonly IOptionsMonitor<KeyStoreOptions> _keyStoreOptions;
    private readonly Dictionary<string, string> _secretStorage = new();
    private readonly AlchemyPayAesSignStrategy _alchemyPayAesSignStrategy;

    public StorageProvider(IOptionsMonitor<KeyStoreOptions> keyStoreOptions, ILogger<StorageProvider> logger,
        AlchemyPayAesSignStrategy alchemyPayAesSignStrategy)
    {
        _keyStoreOptions = keyStoreOptions;
        _logger = logger;
        _alchemyPayAesSignStrategy = alchemyPayAesSignStrategy;
        LoadEncryptDataWithProvidedPassword();
        LoadKeyStoreWithPassword();
    }


    private void AssertKeyMod(string key, Mod checkMod)
    {
        if (!_keyStoreOptions.CurrentValue.ThirdPart.TryGetValue(key, out var keyOption))
            throw new UserFriendlyException("Secret of key " + key + "not found");

        if (!keyOption.Mod.IsNullOrEmpty() || !keyOption.Mod.ToUpper().Contains(checkMod.ToString().ToUpper()))
            throw new UnauthorizedAccessException("Permission denied");
    }
    
    /// <summary>
    ///     Query readable keys
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    /// <exception cref="UserFriendlyException"></exception>
    public string GetThirdPartSecret(string key)
    {
        AssertKeyMod(key, Mod.R);
        if (_secretStorage.TryGetValue(key, out var secret) || secret.IsNullOrEmpty())
            throw new UserFriendlyException("Secret of key " + key + "not found");
        return secret!;
    }

    /// <summary>
    ///     Select custom policy to perform key calculation strategy
    /// </summary>
    /// <param name="key"></param>
    /// <param name="executeInput"></param>
    /// <param name="strategy"></param>
    /// <typeparam name="TInput"></typeparam>
    /// <typeparam name="TOutput"></typeparam>
    /// <returns></returns>
    /// <exception cref="UserFriendlyException"></exception>
    public TOutput ExecuteThirdPartSecret<TInput, TOutput>(string key, TInput executeInput,
        IThirdPartExecuteStrategy<TInput, TOutput> strategy)
        where TInput : BaseThirdPartExecuteInput
    {
        AssertKeyMod(key, Mod.X);
        if (!_secretStorage.TryGetValue(key, out var secret) || secret.IsNullOrEmpty())
            throw new UserFriendlyException("Secret of key " + key + "not found");
        
        return strategy.Execute(secret, executeInput);
    }


    private void LoadEncryptDataWithProvidedPassword()
    {
        if (_keyStoreOptions.CurrentValue.Path.IsNullOrEmpty()) return;
        if (_keyStoreOptions.CurrentValue.ThirdPart.IsNullOrEmpty()) return;

        // Decrypt with password provided in Option
        // Enter password to decrypt
        foreach (var (key, config) in _keyStoreOptions.CurrentValue.ThirdPart)
        {
            if (config.Password.IsNullOrEmpty()) continue;
            var file = $"/{key}.json";
            var path = PathHelper.ResolvePath(_keyStoreOptions.CurrentValue.Path + file);
            var keyStoreContent = InputHelper.ReadFile(path);
            var encrypt = JsonConvert.DeserializeObject<EncryptDataDto>(keyStoreContent);
            var decryptSuccess = encrypt!.TryDecrypt(config.Password, out var secretData, out var msg);
            if (!decryptSuccess)
            {
                _logger.LogWarning("Decrypt ThirdPart apikey data {Key}.json failed: {Message}", key, msg);
                continue;
            }

            _secretStorage[key] = secretData;
            _logger.LogWarning(
                "Decrypt ThirdPart apikey data {File} with provided password, Information:{EncryptInformation}",
                file, encrypt.Information);
        }
    }

    private void LoadKeyStoreWithPassword()
    {
        if (_keyStoreOptions.CurrentValue.Path.IsNullOrEmpty()) return;
        if (_keyStoreOptions.CurrentValue.ThirdPart.IsNullOrEmpty()) return;

        var jsonContents = new Dictionary<string, EncryptDataDto>();
        foreach (var (key, _) in _keyStoreOptions.CurrentValue.ThirdPart)
        {
            if (_secretStorage.ContainsKey(key)) continue;
            var path = PathHelper.ResolvePath(_keyStoreOptions.CurrentValue.Path + $"/{key}.json");
            var keyStoreContent = InputHelper.ReadFile(path);
            var encrypt = JsonConvert.DeserializeObject<EncryptDataDto>(keyStoreContent);
            jsonContents[key] = encrypt;
        }

        if (jsonContents.IsNullOrEmpty()) return;

        Console.WriteLine();
        Console.WriteLine();
        Console.WriteLine();
        Console.WriteLine();
        Console.WriteLine("aaaaaaaaaaaaaaaaaaaaaa Decode ThirdPart apikey aaaaaaaaaaaaaaaaaaaaa");
        Console.WriteLine();
        Console.WriteLine("  Json files will be loaded: ");
        foreach (var (key, encryptData) in jsonContents)
        {
            var file = $"{key}.json";
            var info = encryptData.Information.IsNullOrEmpty() ? "(no info)" : encryptData.Information;
            Console.WriteLine($"     {file} - {info} ");
        }

        Console.WriteLine();
        Console.WriteLine("aaaaaaaaaaaaaaaa  Press [Enter] to decode keystore  aaaaaaaaaaaaaaaa");
        while (ConsoleKey.Enter != Console.ReadKey(true).Key)
        {
            // do nothing
        }

        foreach (var encryptData in jsonContents.Values)
        {
            var inputLabel = $"Input password of {encryptData.Key}.json [Hidden]: ";
            string secretData;
            while (!encryptData.TryDecrypt(InputHelper.ReadPassword(inputLabel), out secretData, out var msg))
            {
                Console.WriteLine($"Failed: {msg}");
            }

            _secretStorage[encryptData.Key] = secretData;
            Console.WriteLine("Success!");
        }

        Console.WriteLine();
    }
}