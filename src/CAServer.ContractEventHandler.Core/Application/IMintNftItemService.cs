using System;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using CAServer.Entities.Es;
using CAServer.EnumType;
using CAServer.FreeMint.Dtos;
using CAServer.FreeMint.Etos;
using CAServer.Grains.Grain.ApplicationHandler;
using CAServer.Grains.Grain.FreeMint;
using Microsoft.Extensions.Logging;
using Orleans;
using Volo.Abp.DependencyInjection;
using IObjectMapper = Volo.Abp.ObjectMapping.IObjectMapper;

namespace CAServer.ContractEventHandler.Core.Application;

public interface IMintNftItemService
{
    Task MintAsync(FreeMintEto eventData);
}

public class MintNftItemService : IMintNftItemService, ISingletonDependency
{
    private readonly INESTRepository<FreeMintIndex, string> _freeMintRepository;
    private readonly ILogger<MintNftItemService> _logger;
    private readonly IObjectMapper _objectMapper;
    private readonly IClusterClient _clusterClient;

    private readonly IFreeMintNftProvider _freeMintNftProvider;

    public MintNftItemService(INESTRepository<FreeMintIndex, string> freeMintRepository,
        ILogger<MintNftItemService> logger, IObjectMapper objectMapper, IClusterClient clusterClient,
        IFreeMintNftProvider freeMintNftProvider)
    {
        _freeMintRepository = freeMintRepository;
        _logger = logger;
        _objectMapper = objectMapper;
        _clusterClient = clusterClient;
        _freeMintNftProvider = freeMintNftProvider;
    }

    public async Task MintAsync(FreeMintEto eventData)
    {
        try
        {
            _logger.LogInformation("[FreeMint] begin handle mint event.");
            var index = await _freeMintRepository.GetAsync(eventData.ConfirmInfo.ItemId);
            if (index == null)
            {
                index = new FreeMintIndex
                {
                    CreateTime = DateTime.UtcNow,
                    UpdateTime = DateTime.UtcNow,
                    Id = eventData.ConfirmInfo.ItemId,
                    Symbol = $"{eventData.CollectionInfo.CollectionName.ToUpper()}-{eventData.ConfirmInfo.TokenId}"
                };
                _objectMapper.Map(eventData.ConfirmInfo, index);
                index.CollectionInfo =
                    _objectMapper.Map<FreeMintCollectionInfo, CollectionInfo>(eventData.CollectionInfo);
                await _freeMintRepository.AddOrUpdateAsync(index);
            }
            else
            {
                _objectMapper.Map(eventData.ConfirmInfo, index);
                index.UpdateTime = DateTime.UtcNow;
            }
            
            await _freeMintRepository.AddOrUpdateAsync(index);
            var transactionInfo = await _freeMintNftProvider.SendMintNftTransactionAsync(eventData);

            index.TransactionInfos.Add(new MintTransactionInfo()
            {
                BeginTime = DateTime.UtcNow,
                EndTime = DateTime.UtcNow,
                BlockTime = transactionInfo.TransactionResultDto.BlockNumber,
                TransactionId = transactionInfo.TransactionResultDto.TransactionId,
                TransactionResult = transactionInfo.TransactionResultDto.Status,
                ErrorMessage = transactionInfo.TransactionResultDto.Error
            });

            var status = transactionInfo.TransactionResultDto.Status == TransactionState.Mined
                ? FreeMintStatus.SUCCESS
                : FreeMintStatus.FAIL;
            var grain = _clusterClient.GetGrain<IFreeMintGrain>(eventData.UserId);
            await grain.ChangeMintStatus(index.Id, status);
            index.Status = status.ToString();
            await _freeMintRepository.AddOrUpdateAsync(index);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "[FreeMint] error");
        }
    }
}