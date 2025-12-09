using System;
using System.Threading.Tasks;
using AElf;
using AElf.Contracts.MultiToken;
using AElf.Indexing.Elasticsearch;
using AElf.Types;
using CAServer.Commons;
using CAServer.Entities.Es;
using CAServer.Grains.Grain.ImTransfer;
using CAServer.ImTransfer.Dtos;
using CAServer.ImTransfer.Etos;
using CAServer.ThirdPart.Dtos;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Orleans;
using Volo.Abp;
using Volo.Abp.Auditing;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.Users;

namespace CAServer.ImTransfer;

[RemoteService(false), DisableAuditing]
public class ImTransferAppService : CAServerAppService, IImTransferAppService
{
    private readonly IClusterClient _clusterClient;
    private readonly INESTRepository<TransferIndex, string> _transferIndexRepository;
    private readonly IDistributedEventBus _distributedEventBus;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ImTransferAppService(IClusterClient clusterClient,
        IDistributedEventBus distributedEventBus,
        INESTRepository<TransferIndex, string> transferIndexRepository,
        IHttpContextAccessor httpContextAccessor)
    {
        _distributedEventBus = distributedEventBus;
        _clusterClient = clusterClient;
        _transferIndexRepository = transferIndexRepository;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<ImTransferResponseDto> TransferAsync(ImTransferDto input)
    {
        try
        {
            var userId = CurrentUser.GetId();
            Logger.LogInformation(
                "Transfer start, userId:{userId}, toUserId:{toUserId}, groupType:{groupType}, channelUuid:{channelUuid}, chainId:{chainId}, rawTransaction:{rawTransaction}",
                userId, input.ToUserId, input.Type, input.ChannelUuid, input.ChainId, input.RawTransaction);

            var transaction =
                Transaction.Parser.ParseFrom(ByteArrayHelper.HexStringToByteArray(input.RawTransaction));
            var transferInput = GetTransferInput(transaction);

            var transactionId = transaction.GetHash().ToHex();
            var transferId = HashHelper.ComputeFrom(transactionId).ToHex();
            var transferGrainDto = ObjectMapper.Map<ImTransferDto, TransferGrainDto>(input);
            transferGrainDto.SenderId = CurrentUser.GetId();
            transferGrainDto.Amount = transferInput.Amount;
            transferGrainDto.Symbol = transferInput.Symbol;
            transferGrainDto.Memo = transferInput.Memo;

            var grain = _clusterClient.GetGrain<IImTransferGrain>(transferId);
            var createResult = await grain.CreateTransfer(transferGrainDto);
            if (!createResult.Success)
            {
                throw new UserFriendlyException(createResult.Message);
            }

            Logger.LogInformation("create transfer success, createResult is {createResult}",
                JsonConvert.SerializeObject(createResult));

            var authToken = GetTokens();
            var transferIndex = ObjectMapper.Map<TransferGrainDto, TransferIndex>(createResult.Data);
            transferIndex.TransactionStatus = TransferTransactionStatus.Processing.ToString();
            transferIndex.SenderRelationToken = authToken.relationToken;
            transferIndex.SenderPortkeyToken = authToken.portkeyToken;
            transferIndex.Message = input.Message;
            transferIndex.TransactionId = transactionId;
            await _transferIndexRepository.AddOrUpdateAsync(transferIndex);
            Logger.LogInformation("transferIndex AddOrUpdateAsync success, transferIndex is {transferIndex}",
                JsonConvert.SerializeObject(transferIndex));

            await _distributedEventBus.PublishAsync(ObjectMapper.Map<TransferIndex, TransferEto>(transferIndex), false,
                false);

            return new ImTransferResponseDto()
            {
                TransferId = transferId
            };
        }
        catch (UserFriendlyException e)
        {
            Logger.LogError(e,
                "Transfer error, toUserId:{toUserId}, groupType:{groupType}, channelUuid:{channelUuid}, chainId:{chainId}, rawTransaction:{rawTransaction}",
                input.ToUserId, input.Type, input.ChannelUuid, input.ChainId, input.RawTransaction);
            throw;
        }
        catch (Exception e)
        {
            Logger.LogError(e,
                "Transfer error, toUserId:{toUserId}, groupType:{groupType}, channelUuid:{channelUuid}, chainId:{chainId}, rawTransaction:{rawTransaction}",
                input.ToUserId, input.Type, input.ChannelUuid, input.ChainId, input.RawTransaction);
            throw;
        }
    }

    public async Task<TransferResultDto> GetTransferResultAsync(string transferId)
    {
        var transfer = await _transferIndexRepository.GetAsync(transferId);
        return ObjectMapper.Map<TransferIndex, TransferResultDto>(transfer);
    }

    private TransferInput GetTransferInput(Transaction transaction)
    {
        var forwardCallDto =
            ManagerForwardCallDto<TransferInput>.Decode(transaction);

        TransferInput? transferInput;
        if (forwardCallDto == null
            || forwardCallDto.MethodName != AElfContractMethodName.Transfer
            || (transferInput = forwardCallDto.ForwardTransactionArgs?.Value as TransferInput) == null)
        {
            throw new UserFriendlyException("Not Transfer-ManagerForwardCall transaction");
        }

        if (transferInput.Amount == 0)
        {
            throw new UserFriendlyException("Amount can not be zero");
        }

        return transferInput;
    }

    private (string portkeyToken, string relationToken) GetTokens()
    {
        var portkeyToken = _httpContextAccessor.HttpContext?.Request.Headers[CommonConstant.AuthHeader];
        var relationToken = _httpContextAccessor.HttpContext?.Request.Headers[ImConstant.RelationAuthHeader];
        if (string.IsNullOrEmpty(relationToken))
        {
            throw new UserFriendlyException("Relation token not found");
        }

        return (portkeyToken, relationToken);
    }
}