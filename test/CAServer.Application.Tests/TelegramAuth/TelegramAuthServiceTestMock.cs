using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http.Headers;
using AElf.Types;
using CAServer.Common;
using CAServer.Telegram.Options;
using CAServer.Verifier;
using Microsoft.Extensions.Options;
using Moq;
using Nest;
using Portkey.Contracts.CA;

namespace CAServer.TelegramAuth;

public partial class TelegramAuthServiceTests
{
    private static IOptionsSnapshot<TelegramAuthOptions> MockTelegramAuthOptionsSnapshot()
    {
        var mockOptionsSnapshot = new Mock<IOptionsSnapshot<TelegramAuthOptions>>();

        mockOptionsSnapshot.Setup(o => o.Value).Returns(
            new TelegramAuthOptions
            {
                BotId = "6741218435",
                BotName = "sTestBBot",
                RedirectUrl = new Dictionary<string, string>()
                {
                    { "portkey", "XXX" },
                    { "openlogin", "XXX" }
                },
                BaseUrl = "http:127.0.0.1:8080",
                Timeout = 10
            });
        return mockOptionsSnapshot.Object;
    }

    private static IContractProvider MockContractProvider()
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

    private static IHttpService MockHttpService()
    {
        var mock = new Mock<IHttpService>();
        mock.Setup(o =>
                o.PostResponseAsync<ResponseResultDto<string>>(It.IsAny<string>(),
                    It.IsAny<Dictionary<string, string>>(), It.IsAny<string>(), It.IsAny<HttpStatusCode>(),
                    It.IsAny<AuthenticationHeaderValue>()))
            .ReturnsAsync((string url, Dictionary<string, string> param, string version, HttpStatusCode code,
                AuthenticationHeaderValue value) =>
            {
                if (param["UserName"].IsNullOrWhiteSpace())
                {
                    return new ResponseResultDto<string>
                    {
                        Success = false,
                        Message = "Error",
                        Data = null
                    };
                }

                return new ResponseResultDto<string>
                {
                    Success = true,
                    Message = null,
                    Data =
                        "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9.eyJ1c2VySWQiOiI2NjMwODY1MzUyIiwidXNlck5hbWUiOiJUaW1tIER1bmNhbm4iLCJhdXRoRGF0ZSI6IjE4MDQ4ODU2MTUiLCJmaXJzdE5hbWUiOiJUaW0iLCJsYXN0TmFtZSI6IkR1bmNhbiIsImhhc2giOiIwZThkNGU1NzBkOTg2ZmRmZmIxYjE2MWRhMWFjNGZhNTM2NGExMmI4YjZkZTQ4ODczOTYxNzQyNWE0Y2YzMTI0IiwicHJvdG9VcmwiOiJhYWEiLCJuYmYiOjE3MDU0NzA1MzMsImV4cCI6MTcwNTQ3NDEzMywiaXNzIjoiUG9ydEtleSIsImF1ZCI6IlBvcnRLZXkifQ.TWty_AtG6QNRO3CEmucEhl-m8DG6xQ6WQcNJHmc6IPkhTRtfRdvHgvXI8eFomGi8DLJ7jm0PWI_kHz3MjiPTG4ECUz7MFNmPCTnNIm7Kcn7FewLVXFgJ1Hmi3n_pDOqrUmTxlIo5z3O4M0qJuX5K-XBJ0U_AafrSDQwYyQCkUHiU1pqyW7I7vPGstET6_LxvszVpa4nnDol-osR21qQKxPemsbkSOZqZoaAt1ei6AQwSX43Upm6SZ155xJeNLtPHZXHmMmrGUaWOhkskWCz8jzr9_gaQjv3zoAbTMPH5CzDYTGCQVML9Hx1VBFEEcXpWBlKEsxXoeVr5rsfVl7MaCQ"
                };
            });

        return mock.Object;
    }

    private IHttpClientService MockHttpClientService()
    {
        var mock = new Mock<IHttpClientService>();
        mock.Setup(o => o.PostAsync<ResponseResultDto<string>>(It.IsAny<string>(), It.IsAny<object>())).ReturnsAsync(
            (string url, object param) =>
            {
                if (((Dictionary<string, string>) param)["Hash"] == "9bf75d2841333abb70a417289921cee27c5744a2158e6345176e369d4c48XXXX")
                {
                    return new ResponseResultDto<string>
                    {
                        Success = false,
                        Message = "",
                        Data = null
                    };
                }
                return new ResponseResultDto<string>
                {
                    Success = true,
                    Message = "",
                    Data = "1234567890"
                };;
            });
        return mock.Object;
    }
}