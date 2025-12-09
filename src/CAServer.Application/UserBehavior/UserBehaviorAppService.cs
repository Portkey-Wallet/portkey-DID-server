using System;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using CAServer.Entities.Es;
using CAServer.UserBehavior.Etos;
using DnsClient.Protocol;
using Microsoft.Extensions.Logging;
using Volo.Abp.ObjectMapping;

namespace CAServer.UserBehavior;

public class UserBehaviorAppService : CAServerAppService, IUserBehaviorAppService
{
    private readonly INESTRepository<UserBehaviorIndex, string> _userBehaviorRepository;
    private readonly IObjectMapper _objectMapper;
    private readonly ILogger<UserBehaviorAppService> _logger;

    public UserBehaviorAppService(
        INESTRepository<UserBehaviorIndex, string> userBehaviorRepository,
        IObjectMapper objectMapper,
        ILogger<UserBehaviorAppService> logger)
    {
        _userBehaviorRepository = userBehaviorRepository;
        _objectMapper = objectMapper;
        _logger = logger;
    }

    public async Task AddUserBehaviorAsync(UserBehaviorEto userBehaviorEto)
    {
        if (string.IsNullOrWhiteSpace(userBehaviorEto.ChainId) || string.IsNullOrWhiteSpace(userBehaviorEto.SessionId))
        {
            return;
        }

        switch (userBehaviorEto.Action)
        {
            case UserBehaviorAction.NewRecord:
                await CreatePartUserBehaviorAsync(userBehaviorEto);
                break;
            case UserBehaviorAction.Register:
                await UpdateUserBehaviorAsync(userBehaviorEto);
                break;
        }
    }
    
    public async Task UpdateUserBehaviorAsync(UserBehaviorEto userBehaviorEto)
    {
        if (string.IsNullOrWhiteSpace(userBehaviorEto.ChainId) || string.IsNullOrWhiteSpace(userBehaviorEto.SessionId))
        {
            return;
        }
        
        var userBehaviorIndex = await _userBehaviorRepository.GetAsync(userBehaviorEto.SessionId);
        if (userBehaviorIndex == null)
        {
            return;
        }
        
        userBehaviorIndex.Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        userBehaviorIndex.CaAddress = userBehaviorEto.CaAddress;
        userBehaviorIndex.CaHash = userBehaviorEto.CaHash;
        userBehaviorIndex.UserId = userBehaviorEto.UserId;
        userBehaviorIndex.Result = userBehaviorEto.Result;
        userBehaviorIndex.Action = userBehaviorEto.Action.ToString();
        userBehaviorIndex.ChainId = userBehaviorEto.ChainId;
        
        await _userBehaviorRepository.UpdateAsync(userBehaviorIndex);
        _logger.LogInformation(
            "UpdateUserBehaviorAsync success, sessionId: {sessionId},cahash:{cahash},Action:{Action}",
            userBehaviorEto.SessionId, userBehaviorIndex.CaHash, userBehaviorIndex.Action);
    }

    public async Task CreatePartUserBehaviorAsync(UserBehaviorEto userBehaviorEto)
    {
        var userBehaviorIndex = new UserBehaviorIndex();
        userBehaviorIndex.Id = userBehaviorEto.SessionId;
        userBehaviorIndex.Referer = userBehaviorEto.Referer;
        userBehaviorIndex.UserAgent = userBehaviorEto.UserAgent;
        userBehaviorIndex.Origin = userBehaviorEto.Origin;
        userBehaviorIndex.Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        userBehaviorIndex.DappName = GetDappName(userBehaviorEto);
        userBehaviorIndex.Device = GetDevice(userBehaviorEto);

        await _userBehaviorRepository.AddOrUpdateAsync(userBehaviorIndex);
        _logger.LogInformation(
            "CreatePartUserBehaviorAsync success, sessionId: {sessionId},Device:{Device},DappName:{DappName}",
            userBehaviorEto.SessionId, userBehaviorIndex.Device, userBehaviorIndex.DappName);

    }

    private string GetDappName(UserBehaviorEto userBehaviorEto)
    {
        var dappName = ParseReferer(userBehaviorEto.Referer);
        if (!string.IsNullOrWhiteSpace(dappName))
        {
            return dappName;
        }

        return UserBehaviorConst.Unknown;
    }

    private string GetDevice(UserBehaviorEto userBehaviorEto)
    {
        var device = ParseUserAgent(userBehaviorEto.UserAgent);
        if (!string.IsNullOrWhiteSpace(device))
        {
            return device;
        }

        return UserBehaviorConst.Unknown;
    }

    private string ParseReferer(string referer)
    {
        if (string.IsNullOrWhiteSpace(referer))
        {
            return null;
        }

        var uri = new Uri(referer);
        string dapp;
        if (UserBehaviorConst.HostMapping.TryGetValue(uri.Host, out dapp))
        {
            return dapp;
        }

        return null;
    }

    private string ParseUserAgent(string userAgent)
    {
        if (string.IsNullOrWhiteSpace(userAgent))
        {
            return null;
        }

        if (userAgent.ToLower().Contains("android"))
        {
            return "android";
        }

        if (userAgent.ToLower().Contains("iphone"))
        {
            return "iphone";
        }

        return "web";
    }
}