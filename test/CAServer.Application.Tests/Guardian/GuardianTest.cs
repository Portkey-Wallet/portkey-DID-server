using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf;
using AElf.Indexing.Elasticsearch;
using CAServer.CAAccount.Dtos;
using CAServer.Entities.Es;
using CAServer.Grains.Grain.ApplicationHandler;
using CAServer.Grains.Grain.Guardian;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Orleans.TestingHost;
using Shouldly;
using Volo.Abp.Validation;
using Xunit;

namespace CAServer.Guardian;

[Collection(CAServerTestConsts.CollectionDefinitionName)]
public partial class GuardianTest : CAServerApplicationTestBase
{
    private readonly IGuardianAppService _guardianAppService;
    private readonly INESTRepository<GuardianIndex, string> _guardiansRepository;
    private readonly TestCluster _cluster;

    private readonly string _identifierHash = "03785a7eb80598f50d179e68da793e72152c94114f196486f1ff34ee6b294fd0";
    private readonly string _identifier = "test@163.com";


    public GuardianTest()
    {
        _guardianAppService = GetService<IGuardianAppService>();
        _guardiansRepository = GetService<INESTRepository<GuardianIndex, string>>();
        _cluster = Cluster;
    }

    protected override void AfterAddApplication(IServiceCollection services)
    {
        services.AddSingleton(GetGuardianProviderMock());
        services.AddSingleton(GetMockAppleUserProvider());
        services.AddSingleton(GetChainOptions());
    }

    [Fact]
    public async Task GetGuardianIdentifiersTest()
    {
        var guardianIdentifier = "test@163.com";
        await _guardiansRepository.AddOrUpdateAsync(new GuardianIndex
        {
            Id = guardianIdentifier,
            Identifier = guardianIdentifier,
            IdentifierHash = HashHelper.ComputeFrom("123").ToHex(),
            Salt = "Salt"
        });

        var result = await _guardianAppService.GetGuardianIdentifiersAsync(new GuardianIdentifierDto
        {
            GuardianIdentifier = _identifier,
            ChainId = "TEST"
        });

        result.ShouldNotBeNull();
        result.GuardianList.Guardians.Count.ShouldBe(1);
    }

    [Fact]
    public async Task GetGuardianIdentifiers_Not_Exist_Test()
    {
        var guardianIdentifier = "test@163.com";
        try
        {
            await _guardianAppService.GetGuardianIdentifiersAsync(new GuardianIdentifierDto
            {
                GuardianIdentifier = guardianIdentifier,
                ChainId = "TEST"
            });
        }
        catch (Exception e)
        {
            e.Message.ShouldBe($"{guardianIdentifier} not exist.");
        }
    }

    [Fact]
    public async Task GetRegisterInfo_Invalid_Params_Test()
    {
        try
        {
            await _guardianAppService.GetRegisterInfoAsync(new RegisterInfoDto()
            {
                LoginGuardianIdentifier = string.Empty,
                CaHash = string.Empty
            });
        }
        catch (Exception e)
        {
            Assert.True(e is AbpValidationException);
        }
    }

    [Fact]
    public async Task GetRegisterInfo_Test()
    {
        var grain = _cluster.Client.GetGrain<IGuardianGrain>($"Guardian-{_identifier}");
        await grain.AddGuardianAsync(_identifier, "salt", _identifierHash);

        var result = await _guardianAppService.GetRegisterInfoAsync(new RegisterInfoDto()
        {
            LoginGuardianIdentifier = _identifier,
            CaHash = string.Empty
        });

        result.ShouldNotBeNull();
        result.OriginChainId.ShouldBe("TEST");
    }

    [Fact]
    public async Task GetRegisterInfo_Guardian_Not_Exist_Test()
    {
        try
        {
            await _guardianAppService.GetRegisterInfoAsync(new RegisterInfoDto()
            {
                LoginGuardianIdentifier = _identifier,
                CaHash = string.Empty
            });
        }
        catch (Exception e)
        {
            e.Message.ShouldBe("Guardian not exist.");
        }
    }

    private IOptions<ChainOptions> GetChainOptions()
    {
        return new OptionsWrapper<ChainOptions>(
            new ChainOptions
            {
                ChainInfos = new Dictionary<string, Grains.Grain.ApplicationHandler.ChainInfo>
                {
                    { "TEST", new Grains.Grain.ApplicationHandler.ChainInfo { } }
                }
            });
    }
}