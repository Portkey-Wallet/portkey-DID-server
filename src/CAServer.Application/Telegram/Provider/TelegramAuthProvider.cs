using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using AElf;
using CAServer.Telegram.Dtos;
using CAServer.Telegram.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;

namespace CAServer.Telegram.Provider;

public interface ITelegramAuthProvider
{
    Task<bool> ValidateTelegramHashAsync(TelegramAuthDto telegramAuthDto);
}

public class TelegramAuthProvider : ISingletonDependency, ITelegramAuthProvider
{
    private ILogger<TelegramAuthProvider> _logger;
    private readonly TelegramAuthOptions _telegramAuthOptions;

    public TelegramAuthProvider(ILogger<TelegramAuthProvider> logger,
        IOptionsSnapshot<TelegramAuthOptions> telegramAuthOptions)
    {
        _logger = logger;
        _telegramAuthOptions = telegramAuthOptions.Value;
    }

    public async Task<bool> ValidateTelegramHashAsync(TelegramAuthDto telegramAuthDto)
    {
        if (telegramAuthDto.Hash.IsNullOrWhiteSpace())
        {
            _logger.LogError("hash parameter in the telegram callback is null. id={0}", telegramAuthDto.Id);
            return false;
        }

        string token = _telegramAuthOptions.Bots[_telegramAuthOptions.DefaultUsed].Token;
        string dataCheckString =
            $"auth_date={telegramAuthDto.AuthDate}\nfirst_name={telegramAuthDto.FirstName}\nid={telegramAuthDto.Id}\nusername={telegramAuthDto.UserName}";

        var localHash = await GenerateTelegramHashAsync(token, dataCheckString);

        if (!localHash.Equals(telegramAuthDto.Hash))
        {
            _logger.LogError("verification of the telegram information has failed. id={0}", telegramAuthDto.Id);
            return false;
        }

        if (!telegramAuthDto.AuthDate.IsNullOrWhiteSpace())
        {
            //validate auth date
            var expiredUnixTimestamp = (long)DateTime.UtcNow.AddSeconds(-_telegramAuthOptions.Expire)
                .Subtract(new DateTime(1970, 1, 1)).TotalSeconds;
            var authDate = long.Parse(telegramAuthDto.AuthDate);
            if (authDate < expiredUnixTimestamp)
            {
                _logger.LogError("verification of the telegram information has failed, login timeout. id={0}", telegramAuthDto.Id);
                return false;
            }
        }

        return true;
    }

    private Task<string> GenerateTelegramHashAsync(string token, string dataCheckString)
    {
        byte[] tokenBytes = Encoding.UTF8.GetBytes(token);
        byte[] dataCheckStringBytes = Encoding.UTF8.GetBytes(dataCheckString);

        using var hmac = new HMACSHA256(tokenBytes);
        var hashBytes = hmac.ComputeHash(dataCheckStringBytes);
        return Task.FromResult(hashBytes.ToHex());
    }
}