using System;
using System.Reflection;
using System.Threading.Tasks;
using CAServer.Telegram;
using CAServer.Telegram.Dtos;
using CAServer.Verifier;
using Castle.DynamicProxy;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Volo.Abp;
using Xunit;

namespace CAServer.TelegramAuth;

[Collection(CAServerTestConsts.CollectionDefinitionName)]
public partial class TelegramAuthServiceTests : CAServerApplicationTestBase
{
    private readonly ITelegramAuthService _telegramAuthService;
    private readonly IVerifierAppService _verifierAppService;

    private const string Redirect = "portkey";
    private const string BotUserName = "sTestBBot";

    private const string Param =
        "portkey#tgAuthResult=eyJpZCI6NjYzMDg2NTM1MiwiZmlyc3RfbmFtZSI6InBvdHRlciIsInBob3RvX3VybCI6Imh0dHBzOlwvXC90Lm1lXC9pXC91c2VycGljXC8zMjBcL1FldkhBcGFUbTR3V2tOb1BhZGxfOGkwaUpWeEw5ZkdKZk1tbW9tbjg0YklxX3lIbzFuU0VsSHl0NmlPZjVEdlEuanBnIiwiYXV0aF9kYXRlIjoxNzAyOTgyNjUwLCJoYXNoIjoiZDMxYTllYTEwMDdmZjcxYzdlNWMyOGVhMzMzYjdmMmUyMDU2YWQ2OGIwYTZiMDA0NzdlYTI2MmFjZWNiZTk4MyJ9";

    private const string ErrorParam =
        "portkey?tgAuthResult=eyJpZCI6NjYzMDg2NTM1MiwiZmlyc3RfbmFtZSI6InBvdHRlciIsInBob3RvX3VybCI6Imh0dHBzOlwvXC90Lm1lXC9pXC91c2VycGljXC8zMjBcL1FldkhBcGFUbTR3V2tOb1BhZGxfOGkwaUpWeEw5ZkdKZk1tbW9tbjg0YklxX3lIbzFuU0VsSHl0NmlPZjVEdlEuanBnIiwiYXV0aF9kYXRlIjoxNzAyOTgyNjUwLCJoYXNoIjoiZDMxYTllYTEwMDdmZjcxYzdlNWMyOGVhMzMzYjdmMmUyMDU2YWQ2OGIwYTZiMDA0NzdlYTI2MmFjZWNiZTk4MyJ9";

    public TelegramAuthServiceTests()
    {
        _telegramAuthService = GetRequiredService<ITelegramAuthService>();
        // _verifierAppService = GetRequiredService<IVerifierAppService>();
    }

    protected override void BeforeAddApplication(IServiceCollection services)
    {
    }

    protected override void AfterAddApplication(IServiceCollection services)
    {
        services.AddSingleton(MockTelegramAuthOptionsSnapshot());
        services.AddSingleton(MockContractProvider());
        services.AddSingleton(MockHttpService());
        services.AddSingleton(MockHttpClientService());
    }

    [Fact]
    public async Task GetTelegramBotInfoAsync_Test()
    {
        var telegramBot = await _telegramAuthService.GetTelegramBotInfoAsync();
        telegramBot.BotName.ShouldBe(BotUserName);
    }

    [Fact]
    public async Task ValidateTelegramHashAndGenerateTokenAsync_Test()
    {
        var request = new TelegramAuthReceiveRequest();

        await Assert.ThrowsAsync<UserFriendlyException>(async () =>
        {
            await _telegramAuthService.ValidateTelegramHashAndGenerateTokenAsync(request);
        });

        request = new TelegramAuthReceiveRequest
        {
            Id = "6637755486",
            UserName = "",
            Auth_Date = "1703473471",
            First_Name = "FirstName",
            Last_Name = "LastName",
            Hash = "9bf75d2841333abb70a417289921cee27c5744a2158e6345176e369d4c48XXXX",
            Photo_Url = "xxxx.jpg"
        };

        await Assert.ThrowsAsync<UserFriendlyException>(async () =>
        {
            await _telegramAuthService.ValidateTelegramHashAndGenerateTokenAsync(request);
        });

        request.UserName = "UserName";
        request.Hash = "6bd8fdba423123065b80c0d05616a789f4ae3574c648f3e6c06823fa58b526ad";

        var telegramToken = await _telegramAuthService.ValidateTelegramHashAndGenerateTokenAsync(request);
        telegramToken.ShouldNotBeNull();
    }
}