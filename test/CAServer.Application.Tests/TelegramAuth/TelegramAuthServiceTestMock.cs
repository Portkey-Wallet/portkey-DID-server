using System.Collections.Generic;
using AElf.Types;
using CAServer.Common;
using CAServer.Telegram.Options;
using Microsoft.Extensions.Options;
using Moq;
using Portkey.Contracts.CA;

namespace CAServer.TelegramAuth;

public partial class TelegramAuthServiceTests
{
    private IOptionsSnapshot<JwtTokenOptions> MockJwtTokenOptionsSnapshot()
    {
        var mockOptionsSnapshot = new Mock<IOptionsSnapshot<JwtTokenOptions>>();
        mockOptionsSnapshot.Setup(o => o.Value).Returns(
            new JwtTokenOptions
            {
                PrivateKey =
                    "MIIEvQIBADANBgkqhkiG9w0BAQEFAASCBKcwggSjAgEAAoIBAQDA4t/KYdhMvDJA\nAukqD3AxNf84b3X44TJxrn8iIWd2KFGly94lFRJrXlTbv50I4umgL/8+pJK/TSBD\nyP42alkXD7qVIYXq9RzB8pMY1wNVMb3heneFuY9onEwMxRiRZ0qSKTzSJ31hOWJ/\nFhCuSMQ7QEZoUzUb7K9Ifno55Ktpd7TtfT9ntMOi1RHOV22eXBdh/BjF4ZWIDAob\n68qk1q2p4pXvd4opYJvfUCD5R1sumQKPdsxfec1VlJiYiEA2MQqFZsYbrWv26X4b\nMBXhfx3fAC47WmKWePhVSsWvWuZ4U55WwAvgFWAlGQZLACrOI+hyfKJz+mxk1D7N\nZeyAphDNAgMBAAECggEAWqpqAjHy38L4pdEYc35VQLR+drMh025bbUdWGB92h/pU\nwqetEGYyxchOAAs2D+bFZVgaVQI7vnNM7r/sCnz7ypTIoe/jiHlV3q6cySjMNsAO\nXo3X38JE8CnDr573uOVCDqkWnzwTud8k9tLS3XFz19wTHFe8V5duHxfgcRUVI5nf\n0U2OZA02dyxSUmt2Dt4QMCZj3y0ayJAo0eCSqyJCLf/2zjdfyBQujFggyvdYqldT\nbwjjAuwSai2Qe+NwCMsYHOqkhLOInThkBxmj4SPaD6LmRZcyVkdMH5YqrfHKOr1C\nZx9RJY3BBeI/2cHczF/4FSNgSxr9W0aHS70RKldX+wKBgQD+3ZoowvJUYeqr1880\nVJ9NngnP2zzRZz9bjgDztc8gbfM6Ia5c8kUE6L8B/YfXKgDgAkSkQqSYIQmD+yXR\nl0lMtrjrm3MvP+v7crZWtHKtHMCQCpeCOguJQtYvrK2cd/hI8JcO+PwXFAXz5SgY\no8IudSLnbZaBrDSMawKpdqNkbwKBgQDBvqbWffiEap0kfCMXyoDIqqyoWKRPNNCt\nBmU6TR9VOKBXt8doFLugZjAl6t+CRoCpFzvruVWF3KzuSMurRVUnhGeP7BkAhZpJ\n5CQ1w7CdfRIO2QwbRbTNf2y5+eF8+McoRX/KNzjxR9yFkJLFjlOLpqvYIbDtiDBR\nlsjjXl8UgwKBgH20Rwi7vlIoSVqggq5I3pmlzSkb9Q9DYViC3a1WiuZv1HtjiWCG\njcwyD6OnrIxPR0c1Vy29QPlXsh3KjYimmZygsuxiK5muaXIHzi9NqRIBEGj7EGdG\nwdHdiU7SWH7IHUm8gIT+AThQw+7bo78jZe8MDz8eKhGjIffv/8wdm9E5AoGBAJqA\n1n2AkbnPZ6eb00R6uf7MlMnffpCnlq4Uxo8ax5znfn7VUREAT6wXMdhbGIjEfn/Z\nxnAyKTCKDeIsbTcVIFZWMVxFtJ6E7Mh2RiYV+3dsMQVkM1JgYVHrhyI8UoSdw/yq\nh8EUDrXjT8m3xEQFPzzD2pCONZEHJTk7XNJtBLfPAoGAOPSgMJ65YeYWVoOrWiBT\nuqT4wU9XfOmdKk6rQ54ATBjQEIGuJEYJ6umELxw5Fvax0hCmWohUuqsvkXYXAvke\n4cs7ZwxuG3WwLiNFsaw7Ar+7w4hWQuRsjRDHtvXKSRMf5wMYAjqzqhRB6Bv9bHYs\nahHLONXMzGtqTOa/t5AaFcA=",
                Issuer = "Issuer",
                Audience = "Audience",
                Expire = 3600 * 24 * 365 * 5
            });
        return mockOptionsSnapshot.Object;
    }

    private IOptionsSnapshot<TelegramAuthOptions> MockTelegramAuthOptionsSnapshot()
    {
        var mockOptionsSnapshot = new Mock<IOptionsSnapshot<TelegramAuthOptions>>();

        mockOptionsSnapshot.Setup(o => o.Value).Returns(
            new TelegramAuthOptions
            {
                Bots = new Dictionary<string, TelegramBtoOptions>()
                {
                    {
                        "sTestBBot", new TelegramBtoOptions
                        {
                            BotId = "6741218435",
                            BotName = "sTestBBot",
                            Token = "6741218435:AAEMuSu3u0y4FXVqGPuW1F5RoQ5kudW_0Xs"
                        }
                    }
                },
                RedirectUrl = new Dictionary<string, string>()
                {
                    { "portkey", "XXX" },
                    { "openlogin", "XXX" }
                },
                DefaultUsed = "sTestBBot",
                Expire = 3600 * 24 * 365 * 5
            });
        return mockOptionsSnapshot.Object;
    }
    
    private IContractProvider MockContractProvider()
    {
        var mockContractProvider = new Mock<IContractProvider>();
        mockContractProvider.Setup(o => o.GetVerifierServersListAsync(It.IsAny<string>()))
            .ReturnsAsync(new GetVerifierServersOutput()
                {
                    VerifierServers =
                    {
                        new Portkey.Contracts.CA.VerifierServer()
                        {
                            Id = Hash.LoadFromHex("50986afa3095f66bd590d6ab26218cc2ed2ef4b1f6e7cdab5b3cbb2cd8a540f8"),
                            EndPoints =
                            {
                                "http://127.0.0.1:1122"
                            },
                            VerifierAddresses =
                            {
                                Address.FromBase58("2mBnRTqXMb5Afz4CWM2QakLRVDfaq2doJNRNQT1MXoi2uc6Zy3")
                            }
                            
                        }
                    }
                });
        return mockContractProvider.Object;
    }
}