using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.ExceptionHandler;
using AElf.Indexing.Elasticsearch;
using CAServer.CAActivity.Provider;
using CAServer.Common;
using CAServer.Commons;
using CAServer.Entities.Es;
using CAServer.Grains.Grain.ApplicationHandler;
using CAServer.Grains.Grain.ImTransfer;
using CAServer.ImTransfer.Etos;
using CAServer.Monitor.Interceptor;
using CAServer.Options;
using CAServer.UserAssets;
using CAServer.UserAssets.Provider;
using GraphQL;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Orleans;
using Volo.Abp.DependencyInjection;

namespace CAServer.ContractEventHandler.Core.Application;

public interface IImTransferService
{
    Task TransferAsync(TransferEto transfer);
}

public class ImTransferService : IImTransferService, ISingletonDependency
{
    private readonly ILogger<ImTransferService> _logger;
    private readonly INESTRepository<TransferIndex, string> _transferRepository;
    private readonly INESTRepository<CAHolderIndex, Guid> _caHolderRepository;
    private readonly IHttpClientProvider _httpClientProvider;
    private readonly ImServerOptions _imServerOptions;
    private readonly IContractProvider _contractProvider;
    private readonly IClusterClient _clusterClient;
    private readonly IGraphQLHelper _graphQlHelper;
    private readonly IUserAssetsProvider _userAssetsProvider;

    public ImTransferService(ILogger<ImTransferService> logger,
        INESTRepository<TransferIndex, string> transferRepository, IHttpClientProvider httpClientProvider,
        IOptionsSnapshot<ImServerOptions> imServerOptions, IContractProvider contractProvider,
        IClusterClient clusterClient, IGraphQLHelper graphQlHelper,
        INESTRepository<CAHolderIndex, Guid> caHolderRepository, IUserAssetsProvider userAssetsProvider)
    {
        _logger = logger;
        _transferRepository = transferRepository;
        _httpClientProvider = httpClientProvider;
        _contractProvider = contractProvider;
        _clusterClient = clusterClient;
        _graphQlHelper = graphQlHelper;
        _caHolderRepository = caHolderRepository;
        _userAssetsProvider = userAssetsProvider;
        _imServerOptions = imServerOptions.Value;
    }

    [ExceptionHandler(typeof(Exception),
        Message = "ImTransferService TransferAsync exist error",  
        TargetType = typeof(ExceptionHandlingService), 
        MethodName = nameof(ExceptionHandlingService.HandleExceptionP1))
    ]
    public async Task TransferAsync(TransferEto transfer)
    {
        _logger.LogInformation($"ImTransferService TransferAsync start, transferId: {transfer.Id}", transfer.Id);
        var grain = _clusterClient.GetGrain<IImTransferGrain>(transfer.Id);
        var imTransfer = await grain.GetTransfer();
        if (!imTransfer.Success)
        {
            _logger.LogError("ImTransferService TransferAsync Get im transfer fail, message:{message}, transferInfo:{transferInfo}",
                imTransfer.Message, JsonConvert.SerializeObject(transfer));
            return;
        }

        var transferGrainDto = imTransfer.Data;
        transferGrainDto.Decimal = await GetDecimalAsync(transfer.Symbol);

        await SendTransactionAsync(transferGrainDto);
        var updateResult = await grain.UpdateTransfer(transferGrainDto);
        if (!updateResult.Success)
        {
            _logger.LogError("ImTransferService TransferAsync Update im transfer fail, message:{message}, transferInfo:{transferInfo}",
                imTransfer.Message, JsonConvert.SerializeObject(transfer));
            return;
        }

        await UpdateTransferAndSendMessageAsync(transferGrainDto);
    }

    private async Task SendTransactionAsync(TransferGrainDto transferGrainDto)
    {
        try
        {
            var result =
                await _contractProvider.ForwardTransactionAsync(transferGrainDto.ChainId,
                    transferGrainDto.RawTransaction);
            _logger.LogInformation("Im transfer result: " + "\n{result}",
                JsonConvert.SerializeObject(result, Formatting.Indented));

            transferGrainDto.TransactionResult = result.Status;
            transferGrainDto.BlockHash = result.BlockHash;
            if (result.Status != TransactionState.Mined)
            {
                transferGrainDto.ErrorMessage = $"Transaction status: {result.Status}. Error: {result.Error}";
                transferGrainDto.TransactionStatus = TransferTransactionStatus.Fail;
                _logger.LogWarning(
                    "Im transfer send transaction fail, transferId:{transferId}, errorMessage:{errorMessage}",
                    transferGrainDto.Id, transferGrainDto.ErrorMessage);
                return;
            }

            transferGrainDto.TransactionStatus = TransferTransactionStatus.Success;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Im transfer send transaction error: transferId:{transferId}", transferGrainDto.Id);
            transferGrainDto.TransactionStatus = TransferTransactionStatus.Fail;
            transferGrainDto.ErrorMessage = e.Message;
        }
    }

    [ExceptionHandler(typeof(Exception),
        Message = "ImTransferService UpdateTransferAndSendMessageAsync exist error",  
        TargetType = typeof(ExceptionHandlingService), 
        MethodName = nameof(ExceptionHandlingService.HandleExceptionP1))
    ]
    public async Task UpdateTransferAndSendMessageAsync(TransferGrainDto transferDto)
    {
        _logger.LogInformation("UpdateTransferAndSendMessageAsync {transferInfo}",
                JsonConvert.SerializeObject(transferDto));
            var transferIndex = await _transferRepository.GetAsync(transferDto.Id);
            if (transferIndex == null)
            {
                _logger.LogError("transferIndex not found: {data}",
                    JsonConvert.SerializeObject(transferDto));
                return;
            }

            transferIndex.Decimal = transferDto.Decimal;
            transferIndex.ErrorMessage = transferDto.ErrorMessage;
            transferIndex.TransactionResult = transferDto.TransactionResult;
            transferIndex.TransactionStatus = transferDto.TransactionStatus.ToString();
            transferIndex.BlockHash = transferDto.BlockHash;
            transferIndex.ModificationTime = transferDto.ModificationTime;
            if (transferDto.TransactionStatus != TransferTransactionStatus.Success)
            {
                await _transferRepository.UpdateAsync(transferIndex);
                return;
            }

            var messageRequestDto =
                JsonConvert.DeserializeObject<ImSendMessageRequestDto>(transferIndex.Message);
            var user = await _caHolderRepository.GetAsync(transferDto.ToUserId);
            var sender = await _caHolderRepository.GetAsync(transferDto.SenderId);

            NftInfo nftInfo = null;
            if (transferDto.Decimal == 0)
            {
                var nftDetail = await GetUserNftInfoAsync(transferDto.Symbol);
                nftInfo = nftDetail?.NftInfo;
            }

            messageRequestDto.Content =
                CustomMessageHelper.BuildTransferContent(messageRequestDto.Content, sender?.NickName, user?.NickName,
                    transferIndex,
                    nftInfo);

            transferIndex.Message = JsonConvert.SerializeObject(messageRequestDto);
            await _transferRepository.UpdateAsync(transferIndex);

            var headers = new Dictionary<string, string>
            {
                { ImConstant.RelationAuthHeader, transferIndex.SenderRelationToken },
                { CommonConstant.AuthHeader, transferIndex.SenderPortkeyToken }
            };
            await _httpClientProvider.PostAsync<ImSendMessageResponseDto>(
                _imServerOptions.BaseUrl + ImConstant.SendMessageUrl, messageRequestDto, headers);
    }

    private async Task<int> GetDecimalAsync(string symbol)
    {
        try
        {
            var decimals = await GetTokenDecimalAsync(symbol);
            var tokenInfo = decimals?.TokenInfo?.FirstOrDefault();
            if (tokenInfo == null)
            {
                _logger.LogWarning("get decimal return empty, symbol:{symbol}", symbol);
                return 0;
            }

            return tokenInfo.Decimals;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "get decimal error, symbol:{symbol}", symbol);
            return 0;
        }
    }

    private async Task<IndexerSymbols> GetTokenDecimalAsync(string symbol)
    {
        return await _graphQlHelper.QueryAsync<IndexerSymbols>(new GraphQLRequest
        {
            Query = @"
			    query($symbol:String,$skipCount:Int!,$maxResultCount:Int!) {
                    tokenInfo(dto: {symbol:$symbol,skipCount:$skipCount,maxResultCount:$maxResultCount}){
                        decimals
                    }
                }",
            Variables = new
            {
                symbol, skipCount = 0, maxResultCount = 1
            }
        });
    }

    private async Task<IndexerNftInfo> GetUserNftInfoAsync(string symbol)
    {
        var nftInfo = await _userAssetsProvider.GetUserNftInfoBySymbolAsync(new List<CAAddressInfo>(),
            symbol, 0, 1);

        return nftInfo?.CaHolderNFTBalanceInfo?.Data?.FirstOrDefault();
    }
}