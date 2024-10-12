using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.ExceptionHandler;
using AElf.Indexing.Elasticsearch;
using CAServer.Common;
using CAServer.Commons;
using CAServer.Entities.Es;
using CAServer.EntityEventHandler.Core;
using CAServer.EnumType;
using CAServer.Grains.Grain.RedPackage;
using CAServer.Monitor.Interceptor;
using CAServer.Options;
using CAServer.RedPackage;
using CAServer.RedPackage.Etos;
using Hangfire;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Orleans;
using Volo.Abp.DependencyInjection;

namespace CAServer.ContractEventHandler.Core.Application;

public interface IRedPackageCreateResultService : ISingletonDependency
{
    Task UpdateRedPackageAndSendMessageAsync(RedPackageCreateResultEto redPackageCreateResult);
}

public class RedPackageCreateResultService : IRedPackageCreateResultService
{
    private readonly IClusterClient _clusterClient;
    private readonly ILogger<RedPackageCreateResultService> _logger;
    private readonly INESTRepository<RedPackageIndex, Guid> _redPackageRepository;
    private readonly IHttpClientProvider _httpClientProvider;
    private readonly ImServerOptions _imServerOptions;
    private readonly RedPackageOptions _redPackageOptions;

    public RedPackageCreateResultService(
        ILogger<RedPackageCreateResultService> logger,
        INESTRepository<RedPackageIndex, Guid> redPackageRepository,
        IClusterClient clusterClient,
        IHttpClientProvider httpClientProvider,
        IOptionsSnapshot<ImServerOptions> imServerOptions,
        IOptionsSnapshot<RedPackageOptions> redPackageOptions)
    {
        _logger = logger;
        _redPackageRepository = redPackageRepository;
        _imServerOptions = imServerOptions.Value;
        _clusterClient = clusterClient;
        _httpClientProvider = httpClientProvider;
        _redPackageOptions = redPackageOptions.Value;
    }

    [ExceptionHandler(typeof(Exception),
        Message = "RedPackageCreateResultService RedPackageCreateResultEto exist error",
        TargetType = typeof(ExceptionHandlingService),
        MethodName = nameof(ExceptionHandlingService.HandleExceptionP1))
    ]
    public async Task UpdateRedPackageAndSendMessageAsync(RedPackageCreateResultEto eventData)
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
            redPackageIndex.TransactionStatus = RedPackageTransactionStatus.Fail;
            redPackageIndex.ErrorMessage = eventData.Message;
            var updateRedPackageTask = _redPackageRepository.UpdateAsync(redPackageIndex);
            var grain = _clusterClient.GetGrain<ICryptoBoxGrain>(redPackageIndex.RedPackageId);
            var cancelRedPackageTask = grain.CancelRedPackage();
            await Task.WhenAll(updateRedPackageTask, cancelRedPackageTask);
            return;
        }

        redPackageIndex.TransactionStatus = RedPackageTransactionStatus.Success;
        var updateTask = _redPackageRepository.UpdateAsync(redPackageIndex);
        BackgroundJob.Schedule<RedPackageTask>(x => x.ExpireRedPackageRedPackageAsync(redPackageIndex.RedPackageId),
            TimeSpan.FromMilliseconds(_redPackageOptions.ExpireTimeMs + 30 * 1000));

        if (redPackageIndex.RedPackageDisplayType == null
            || RedPackageDisplayType.Common.Equals(redPackageIndex.RedPackageDisplayType)
            || 0.Equals((int)redPackageIndex.RedPackageDisplayType))
        {
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
        else
        {
            await Task.WhenAll(updateTask);
        }
    }
}