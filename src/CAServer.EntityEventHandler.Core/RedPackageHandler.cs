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
    private readonly IHttpClientProvider _httpClientProvider;
    private readonly ImServerOptions _imServerOptions;

    
    public RedPackageHandler(IObjectMapper objectMapper, ILogger<RedPackageHandler> logger,
        INESTRepository<RedPackageIndex, Guid> redPackageRepository, IImRequestProvider imRequestProvider,
        IClusterClient clusterClient,
        IHttpClientProvider httpClientProvider,
        IOptionsSnapshot<ImServerOptions> imServerOptions,
        IOptionsSnapshot<RedPackageOptions> redPackageOptions)
    {
        _objectMapper = objectMapper;
        _logger = logger;
        _redPackageRepository = redPackageRepository;
        _imRequestProvider = imRequestProvider;
        _redPackageOptions = redPackageOptions.Value;
        _imServerOptions = imServerOptions.Value;
        _clusterClient = clusterClient;
        _httpClientProvider = httpClientProvider;
    }
    
    public async Task HandleEventAsync(RedPackageCreateResultEto eventData)
    {
        try
        {
            
            _logger.LogInformation("RedPackageCreateResultEto {Message}",JsonConvert.SerializeObject(eventData));
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
                await _redPackageRepository.UpdateAsync(redPackageIndex);
                var grain = _clusterClient.GetGrain<IRedPackageGrain>(redPackageIndex.RedPackageId);
                await grain.CancelRedPackage();
                return;
            }

            redPackageIndex.TransactionStatus = RedPackageTransactionStatus.Success;
            
            await _redPackageRepository.UpdateAsync(redPackageIndex);
            _logger.LogInformation("RedPackageCreateResultEto UpdateAsync {redPackageIndex}",redPackageIndex);
            _logger.LogInformation("RedPackageCreate end pay job start");
            BackgroundJob.Schedule<RedPackageTask>(x => x.ExpireRedPackageRedPackageAsync(redPackageIndex.RedPackageId),
                TimeSpan.FromMilliseconds(RedPackageConsts.ExpireTimeMs + 30 *1000));

            //send redpackage Card
            var imSendMessageRequestDto = new ImSendMessageRequestDto();
            try
            {
                imSendMessageRequestDto = JsonConvert.DeserializeObject<ImSendMessageRequestDto>(redPackageIndex.Message);
            }
            catch (Exception e)
            {
                _logger.LogError(e,"RedPackageCreateResultEto Message DeserializeObject fail: {Message}", redPackageIndex.Message);
                imSendMessageRequestDto = new ImSendMessageRequestDto();
                imSendMessageRequestDto.SendUuid = Guid.NewGuid().ToString();
                imSendMessageRequestDto.ChannelUuid = redPackageIndex.ChannelUuid;
                imSendMessageRequestDto.Content = CustomMessageHelper.BuildRedPackageCardContent(_redPackageOptions, redPackageIndex.SenderId,
                    redPackageIndex.Memo, redPackageIndex.RedPackageId);
                imSendMessageRequestDto.Type = RedPackageConsts.RedPackageCardType;
            }
                
            var headers = new Dictionary<string, string>();
            headers.Add(ImConstant.RelationAuthHeader,redPackageIndex.SenderRelationToken);
            headers.Add(CommonConstant.AuthHeader,redPackageIndex.SenderPortkeyToken);
            await _httpClientProvider.PostAsync<ImSendMessageResponseDto>(
                _imServerOptions.BaseUrl + ImConstant.SendMessageUrl, imSendMessageRequestDto, headers);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "RedPackageCreateResultEto handle fail {Message}", JsonConvert.SerializeObject(eventData));
        }
    }
}