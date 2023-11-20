using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using CAServer.Common;
using CAServer.Commons;
using CAServer.Entities.Es;
using CAServer.Grains.Grain.RedPackage;
using CAServer.RedPackage;
using CAServer.RedPackage.Etos;
using Hangfire;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;
using Newtonsoft.Json;
using Orleans;
using Volo.Abp.ObjectMapping;

namespace CAServer.EntityEventHandler.Core;

public class RedPackageHandler:IDistributedEventHandler<RedPackageCreateResultEto>,ITransientDependency
{
    private readonly IObjectMapper _objectMapper;
    private readonly IClusterClient _clusterClient;
    private readonly ILogger<RedPackageHandler> _logger;
    private readonly INESTRepository<RedPackageIndex, Guid> _redPackageRepository;
    private readonly IImRequestProvider _imRequestProvider;
    private readonly RedPackageOptions _redPackageOptions;
    
    public RedPackageHandler(IObjectMapper objectMapper, ILogger<RedPackageHandler> logger,
        INESTRepository<RedPackageIndex, Guid> redPackageRepository, IImRequestProvider imRequestProvider,
        IClusterClient clusterClient,
        IOptionsSnapshot<RedPackageOptions> redPackageOptions)
    {
        _objectMapper = objectMapper;
        _logger = logger;
        _redPackageRepository = redPackageRepository;
        _imRequestProvider = imRequestProvider;
        _redPackageOptions = redPackageOptions.Value;
        _clusterClient = clusterClient;
    }
    
    public async Task HandleEventAsync(RedPackageCreateResultEto eventData)
    {
        try
        {
            var sessionId = eventData.SessionId;
            var redPackageIndex = await _redPackageRepository.GetAsync(sessionId);
            if (redPackageIndex == null)
            {
                _logger.LogError("RedPackageCreateResultEto not found: {Message}", JsonConvert.SerializeObject(eventData));
                return;
            }

            redPackageIndex.TransactionId = eventData.TransactionId;
            redPackageIndex.TransactionResult = eventData.TransactionResult;
            if (eventData.Success == false)
            {
                redPackageIndex.TransactionStatus = RedPackageTransactionStatus.Fail;
                redPackageIndex.ErrorMessage = eventData.Message;
                var grain = _clusterClient.GetGrain<IRedPackageGrain>(redPackageIndex.RedPackageId);
                await grain.CancelRedPackage();
                return;
            }

            redPackageIndex.TransactionStatus = RedPackageTransactionStatus.Success;
            
            await _redPackageRepository.UpdateAsync(redPackageIndex);
            
            BackgroundJob.Schedule<RedPackageTask>(x => x.DeleteRedPackageAsync(redPackageIndex.RedPackageId),
                TimeSpan.FromSeconds(RedPackageConsts.ExpireTime));

            //send redpackage Card
            var imSendMessageRequestDto = new ImSendMessageRequestDto();
            imSendMessageRequestDto.SendUuid = redPackageIndex.SendUuid;
            imSendMessageRequestDto.ChannelUuid = redPackageIndex.ChannelUuid;
            imSendMessageRequestDto.Content = redPackageIndex.Message;
            /*imSendMessageRequestDto.Content = CustomMessageHelper.BuildRedPackageCardContent(_redPackageOptions, redPackageIndex.SenderId,
                redPackageIndex.Memo, redPackageIndex.RedPackageId);*/
            imSendMessageRequestDto.Type = RedPackageConsts.RedPackageCardType;
            var headers = new Dictionary<string, string>();
            headers.Add(ImConstant.RelationAuthHeader,redPackageIndex.SenderRelationToken);
            await _imRequestProvider.PostAsync<object>(ImConstant.SendMessageUrl, imSendMessageRequestDto, headers);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Message}", JsonConvert.SerializeObject(eventData));
        }
    }
}