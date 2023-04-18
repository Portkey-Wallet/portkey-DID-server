using System;
using System.Linq;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using CAServer.Entities.Es;
using CAServer.Grains;
using CAServer.Grains.Grain.Tokens.UserTokens;
using CAServer.Options;
using CAServer.Tokens.Dtos;
using CAServer.Tokens.Etos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans;
using Volo.Abp;
using Volo.Abp.Auditing;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.Users;

namespace CAServer.Tokens;

[RemoteService(IsEnabled = false)]
[DisableAuditing]
public class UserTokenAppService : CAServerAppService, IUserTokenAppService
{
    private readonly IClusterClient _clusterClient;
    private readonly TokenListOptions _tokenListOptions;
    private readonly IDistributedEventBus _distributedEventBus;

    public UserTokenAppService(
        IClusterClient clusterClient,
        IOptionsSnapshot<TokenListOptions> tokenListOptions,
        IDistributedEventBus distributedEventBus)
    {
        _clusterClient = clusterClient;
        _distributedEventBus = distributedEventBus;
        _tokenListOptions = tokenListOptions.Value;
    }

    [Authorize]
    public async Task<UserTokenDto> ChangeTokenDisplayAsync(bool isDisplay, Guid id)
    {
        var grain = _clusterClient.GetGrain<IUserTokenGrain>(id);
        var userId = CurrentUser.GetId();
        var tokenResult = await grain.ChangeTokenDisplayAsync(userId, isDisplay);
        if (!tokenResult.Success)
        {
            throw new UserFriendlyException(tokenResult.Message);
        }

        var toPublish = ObjectMapper.Map<UserTokenGrainDto, UserTokenEto>(tokenResult.Data);
        await _distributedEventBus.PublishAsync(toPublish);
        return ObjectMapper.Map<UserTokenGrainDto, UserTokenDto>(tokenResult.Data);
    }

    public async Task<UserTokenDto> AddUserTokenAsync(Guid userId, AddUserTokenInput input)
    {
        Logger.LogInformation("start to add token.");
        var list = _tokenListOptions.UserToken.Select(async userTokenItem => await InitialUserToken(userId, userTokenItem));
        await Task.WhenAll(list);
        return new UserTokenDto();
    }

    private async Task InitialUserToken(Guid userId, UserTokenItem userTokenItem)
    {
        var userTokenSymbol = _clusterClient.GetGrain<IUserTokenSymbolGrain>(
            GrainIdHelper.GenerateGrainId(userId.ToString("N"), userTokenItem.Token.ChainId,
                userTokenItem.Token.Symbol));
        var ifExist =
            await userTokenSymbol.IsUserTokenSymbolExistAsync(userTokenItem.Token.ChainId,
                userTokenItem.Token.Symbol);
        if (ifExist)
        {
            throw new UserFriendlyException("User token already existed.");
        }

        var userTokenId = GuidGenerator.Create();
        var grain = _clusterClient.GetGrain<IUserTokenGrain>(userTokenId);
        var tokenItem = ObjectMapper.Map<UserTokenItem, UserTokenGrainDto>(userTokenItem);
        var addTokenResult = await grain.AddUserTokenAsync(userId, tokenItem);
        if (!addTokenResult.Success)
        {
            throw new UserFriendlyException(addTokenResult.Message);
        }

        var toPublish = ObjectMapper.Map<UserTokenGrainDto, UserTokenEto>(addTokenResult.Data);
        Logger.LogInformation($"pulish add user token event：{toPublish.UserId}-{toPublish.Token.ChainId}-{toPublish.Token.Symbol}");
        await _distributedEventBus.PublishAsync(toPublish);
    }
}