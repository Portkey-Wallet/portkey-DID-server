using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using CAServer.Common;
using CAServer.Commons;
using CAServer.Entities.Es;
using CAServer.Grains.Grain.RedPackage;
using CAServer.Options;
using CAServer.RedPackage;
using CAServer.RedPackage.Etos;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Orleans;

namespace CAServer.ContractEventHandler.Core.Application;

public interface IRedPackageCreateResultService
{
    Task updateRedPackageAndSengMessageAsync(RedPackageCreateResultEto redPackageCreateResult);
}

public class RedPackageCreateResultService : IRedPackageCreateResultService
{
    private readonly IClusterClient _clusterClient;
    private readonly ILogger<RedPackageCreateResultService> _logger;
    private readonly INESTRepository<RedPackageIndex, Guid> _redPackageRepository;
    private readonly IHttpClientProvider _httpClientProvider;
    private readonly ImServerOptions _imServerOptions;

    public RedPackageCreateResultService(
        ILogger<RedPackageCreateResultService> logger,
        INESTRepository<RedPackageIndex, Guid> redPackageRepository,
        IClusterClient clusterClient,
        IHttpClientProvider httpClientProvider,
        IOptionsSnapshot<ImServerOptions> imServerOptions)
    {
        _logger = logger;
        _redPackageRepository = redPackageRepository;
        _imServerOptions = imServerOptions.Value;
        _clusterClient = clusterClient;
        _httpClientProvider = httpClientProvider;
    }

    public async Task updateRedPackageAndSengMessageAsync(RedPackageCreateResultEto eventData)
    {
        try
        {
            _logger.LogInformation("RedPackageCreateResultEto {Message}", JsonConvert.SerializeObject(eventData));
            var sessionId = eventData.SessionId;
            var redPackageIndex = await _redPackageRepository.GetAsync(sessionId);
            if (redPackageIndex == null)
            {
                _logger.LogError("RedPackageCreateResultEto not found: {Message}",
                    JsonConvert.SerializeObject(eventData));
                return;
            }
        
            redPackageIndex.TransactionId = eventData.TransactionId;
            redPackageIndex.TransactionResult = eventData.TransactionResult;
            if (eventData.Success == false)
            {
                //TODO P3 updating ES data and updating Grain data can be done in parallel.
                redPackageIndex.TransactionStatus = RedPackageTransactionStatus.Fail;
                redPackageIndex.ErrorMessage = eventData.Message;
                var updateRedPackageTask = _redPackageRepository.UpdateAsync(redPackageIndex);
                var grain = _clusterClient.GetGrain<IRedPackageGrain>(redPackageIndex.RedPackageId);
                var cancelRedPackageTask = grain.CancelRedPackage();
                await Task.WhenAll(updateRedPackageTask, cancelRedPackageTask);
                return;
            }
        
            //TODO P1 updating ES data and sending IM messages can be executed in parallel.
            redPackageIndex.TransactionStatus = RedPackageTransactionStatus.Success;
            var updateTask = _redPackageRepository.UpdateAsync(redPackageIndex);
        
            /*BackgroundJob.Schedule<RedPackageTask>(x => x.ExpireRedPackageRedPackageAsync(redPackageIndex.RedPackageId),
                TimeSpan.FromMilliseconds(RedPackageConsts.ExpireTimeMs));*/
        
            //send redpackage Card
            var imSendMessageRequestDto = new ImSendMessageRequestDto();
            try
            {
                imSendMessageRequestDto =
                    JsonConvert.DeserializeObject<ImSendMessageRequestDto>(redPackageIndex.Message);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "RedPackageCreateResultEto Message DeserializeObject fail: {Message}",
                    redPackageIndex.Message);
                imSendMessageRequestDto = new ImSendMessageRequestDto();
                imSendMessageRequestDto.SendUuid = Guid.NewGuid().ToString();
                imSendMessageRequestDto.ChannelUuid = redPackageIndex.ChannelUuid;
                imSendMessageRequestDto.Content = CustomMessageHelper.BuildRedPackageCardContent(
                    redPackageIndex.SenderId,
                    redPackageIndex.Memo, redPackageIndex.RedPackageId);
                imSendMessageRequestDto.Type = RedPackageConsts.RedPackageCardType;
            }
        
            var headers = new Dictionary<string, string>();
            headers.Add(ImConstant.RelationAuthHeader, redPackageIndex.SenderRelationToken);
            headers.Add(CommonConstant.AuthHeader, redPackageIndex.SenderPortkeyToken);
            var sendMessageTask = _httpClientProvider.PostAsync<ImSendMessageResponseDto>(
                _imServerOptions.BaseUrl + ImConstant.SendMessageUrl, imSendMessageRequestDto, headers);
        
            await Task.WhenAll(updateTask, sendMessageTask);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "RedPackageCreateResultEto handle fail {Message}",
                JsonConvert.SerializeObject(eventData));
        }
    }
}