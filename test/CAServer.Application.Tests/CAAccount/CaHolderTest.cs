using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf;
using AElf.Client.Dto;
using AElf.Client.MultiToken;
using AElf.Types;
using CAServer.CAAccount.Dtos;
using CAServer.Dtos;
using CAServer.Grain.Tests;
using CAServer.Grains.Grain.Chain;
using CAServer.Grains.Grain.Contacts;
using CAServer.Security;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson.IO;
using Moq;
using NSubstitute;
using OpenIddict.Abstractions;
using Orleans.TestingHost;
using Portkey.Contracts.CA;
using Shouldly;
using Volo.Abp.Security.Claims;
using Volo.Abp.Users;
using Xunit;
using Xunit.Abstractions;
using JsonConvert = Newtonsoft.Json.JsonConvert;

namespace CAServer.CAAccount;

[Collection(CAServerTestConsts.CollectionDefinitionName)]
public class CaHolderTest : CAServerApplicationTestBase
{
    private readonly INickNameAppService _nickNameAppService;
    private ICurrentUser _currentUser;
    private readonly TestCluster _cluster;
    protected readonly ITestOutputHelper TestOutputHelper;
    private readonly ITransactionFeeAppService _transactionFeeAppService;
    
    public CaHolderTest(ITestOutputHelper testOutputHelper)
    {
        TestOutputHelper = testOutputHelper;
        _nickNameAppService = GetRequiredService<INickNameAppService>();
        _cluster = GetRequiredService<ClusterFixture>().Cluster;
        _transactionFeeAppService = GetRequiredService<ITransactionFeeAppService>();    
    }

    protected override void AfterAddApplication(IServiceCollection services)
    {
        _currentUser = new CurrentUser(new FakeCurrentPrincipalAccessor());
        services.AddSingleton(_currentUser);
    }

    [Fact]
    public async Task SetNicknameTest()
    {
        var grain = _cluster.Client.GetGrain<ICAHolderGrain>(_currentUser.GetId());
        await grain.AddHolderAsync(new CAHolderGrainDto
        {
            UserId = _currentUser.GetId(),
            CaHash = "hash"
        });

        var result = await _nickNameAppService.SetNicknameAsync(new UpdateNickNameDto
        {
            NickName = "Tom"
        });
        
        result.Nickname.ShouldBe("Tom");
    }

    [Fact]
    public  void  CalculateFee_Test()
    {
        
        var chainIds = new List<string>
        {
            "AELF",
            "tDVV"
        };
        var dto = new TransactionFeeDto
        {
            ChainIds = chainIds
        };
        var resultDtos =  _transactionFeeAppService.CalculateFee(dto);
        resultDtos.Count.ShouldBe(2);
    }
}