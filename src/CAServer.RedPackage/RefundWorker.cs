using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using CAServer.ContractEventHandler.Core.Application;
using CAServer.ContractService;
using CAServer.Entities.Es;
using CAServer.Grains.Grain.ApplicationHandler;
using CAServer.Grains.Grain.RedPackage;
using CAServer.RedPackage.Dtos;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nest;
using Newtonsoft.Json;
using Orleans;
using Portkey.Contracts.CryptoBox;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.Threading;
using ChainOptions = CAServer.ContractEventHandler.Core.Application.ChainOptions;

namespace CAServer.RedPackage;

// ReSharper disable once ClassNeverInstantiated.Global
public class RefundWorker : AsyncPeriodicBackgroundWorkerBase
{
    private readonly ILogger<RefundWorker> _logger;
    private readonly IClusterClient _clusterClient;
    private readonly PayRedPackageAccount _packageAccount;
    private readonly INESTRepository<RedPackageIndex, Guid> _redPackageRepository;
    private readonly RefundOptions _refundOptions;
    private readonly ChainOptions _chainOptions;
    private readonly IContractService _contractService;

    public RefundWorker(AbpAsyncTimer timer, IServiceScopeFactory serviceScopeFactory, ILogger<RefundWorker> logger,
        IClusterClient clusterClient, IOptions<PayRedPackageAccount> packageAccount,
        INESTRepository<RedPackageIndex, Guid> redPackageRepository,
        IOptionsSnapshot<RefundOptions> refundOptions, IOptions<ChainOptions> chainOptions,
        IContractService contractService) :
        base(timer, serviceScopeFactory)
    {
        _logger = logger;
        _clusterClient = clusterClient;
        _redPackageRepository = redPackageRepository;
        _contractService = contractService;
        _chainOptions = chainOptions.Value;
        _refundOptions = refundOptions.Value;
        _packageAccount = packageAccount.Value;
        Timer.RunOnStart = true;
        Timer.Period = 1000 * 600000;
    }


    protected override async Task DoWorkAsync(PeriodicBackgroundWorkerContext workerContext)
    {
        _logger.LogInformation("#### begin to handle refund");
        await RefundAsync(Guid.Parse(_refundOptions.PackageId));
        _logger.LogInformation("#### end to handle refund");
    }


    private async Task<bool> RefundAsync(Guid redPackageId)
    {
        try
        {
            var grain = _clusterClient.GetGrain<ICryptoBoxGrain>(redPackageId);

            var redPackageDetail = await grain.GetRedPackage(redPackageId);
            var redPackageDetailDto = redPackageDetail.Data;
            var payRedPackageFrom = _packageAccount.getOneAccountRandom();
            _logger.LogInformation("Refund red package, payRedPackageFrom:{payRedPackageFrom} ",
                payRedPackageFrom);

            var redPackageIndex = await GetRedPackageAsync(redPackageId.ToString());
            if (redPackageIndex.TransactionStatus != RedPackageTransactionStatus.Success)
            {
                var res = await SendTransferRedPacketRefundAsync(redPackageDetailDto,
                    payRedPackageFrom);
                _logger.LogInformation("Refund red package result, transId:{transactionId}, status:{status}",
                    res.TransactionResultDto.TransactionId, res.TransactionResultDto.Status);

                if (res.TransactionResultDto.Status == TransactionState.Mined)
                {
                    await grain.CancelRedPackage();
                    _logger.LogInformation("cancel red package success {redPackageId}", redPackageId);
                    // refresh es status
                    redPackageIndex.TransactionStatus = RedPackageTransactionStatus.Fail;

                    _logger.LogInformation("refresh es success {redPackageId}", redPackageId);
                    await _redPackageRepository.UpdateAsync(redPackageIndex);

                    _logger.LogInformation("Refund red package success {transactionId}",
                        res.TransactionResultDto.TransactionId);
                    return true;
                }

                _logger.LogError("Refund red package fail {message}", res.TransactionResultDto.Error);
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Refund red package fail {message}", e.Message);
        }

        return false;
    }

    public async Task<RedPackageIndex> GetRedPackageAsync(string redPackageId)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<RedPackageIndex>, QueryContainer>>() { };
        mustQuery.Add(q => q.Term(i => i.Field(f => f.RedPackageId).Value(redPackageId)));

        QueryContainer Filter(QueryContainerDescriptor<RedPackageIndex> f) => f.Bool(b => b.Must(mustQuery));
        return await _redPackageRepository.GetAsync(Filter);
    }

    public async Task<TransactionInfoDto> SendTransferRedPacketRefundAsync(RedPackageDetailDto redPackageDetail,
        string payRedPackageFrom)
    {
        var redPackageId = redPackageDetail.Id;
        var chainId = redPackageDetail.ChainId;
        var redPackageKeyGrain = _clusterClient.GetGrain<IRedPackageKeyGrain>(redPackageDetail.Id);
        if (!_chainOptions.ChainInfos.TryGetValue(chainId, out var chainInfo))
        {
            return null;
        }

        var grab = redPackageDetail.Items.Sum(item => long.Parse(item.Amount));
        var sendInput = new RefundCryptoBoxInput
        {
            CryptoBoxId = redPackageId.ToString(),
            Amount = long.Parse(redPackageDetail.TotalAmount) - grab,
            CryptoBoxSignature =
                await redPackageKeyGrain.GenerateSignature(
                    $"{redPackageId}-{long.Parse(redPackageDetail.TotalAmount) - grab}")
        };
        _logger.LogInformation("SendTransferRedPacketRefundAsync input {input}",
            JsonConvert.SerializeObject(sendInput));

        return await _contractService.SendTransferRedPacketToChainAsync(chainId, sendInput, payRedPackageFrom,
            chainInfo.RedPackageContractAddress, MethodName.RefundCryptoBox);
    }
}