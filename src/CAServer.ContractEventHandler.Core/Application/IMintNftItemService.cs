using System;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using CAServer.Commons;
using CAServer.Entities.Es;
using CAServer.EnumType;
using CAServer.FreeMint.Dtos;
using CAServer.FreeMint.Etos;
using CAServer.Grains.Grain.FreeMint;
using Microsoft.Extensions.Logging;
using Orleans;
using Volo.Abp.DependencyInjection;
using Volo.Abp.ObjectMapping;

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

    public MintNftItemService(INESTRepository<FreeMintIndex, string> freeMintRepository,
        ILogger<MintNftItemService> logger, IObjectMapper objectMapper, IClusterClient clusterClient)
    {
        _freeMintRepository = freeMintRepository;
        _logger = logger;
        _objectMapper = objectMapper;
        _clusterClient = clusterClient;
    }

    public async Task MintAsync(FreeMintEto eventData)
    {
        try
        {
            _logger.LogInformation("[FreeMint] begin handle mint event.");
            // save in es
            var index = await _freeMintRepository.GetAsync(eventData.ConfirmInfo.ItemId);
            if (index == null)
            {
                index = new FreeMintIndex
                {
                    CreateTime = DateTime.UtcNow,
                    UpdateTime = DateTime.UtcNow,
                    Id = eventData.ConfirmInfo.ItemId
                };
                _objectMapper.Map(eventData.ConfirmInfo, index);
                index.CollectionInfo =
                    _objectMapper.Map<FreeMintCollectionInfo, CollectionInfo>(eventData.CollectionInfo);
                await _freeMintRepository.AddOrUpdateAsync(index);
            }
            else
            {
                _objectMapper.Map(index, eventData.ConfirmInfo);
                index.UpdateTime = DateTime.UtcNow;
            }
            // send transaction
            // save transaction info into index

            // test
            index.TransactionInfos.Add(new MintTransactionInfo()
            {
                BeginTime = DateTime.UtcNow,
                EndTime = DateTime.UtcNow,
                BlockTime = 1720454400,
                TransactionId = "test",
                TransactionResult = "SUCCESS"
            });
            await Task.Delay(4000);

            var grain = _clusterClient.GetGrain<IFreeMintGrain>(eventData.UserId);
            var changeResult = await grain.ChangeMintStatus(index.Id, FreeMintStatus.SUCCESS);

            index.Status = FreeMintStatus.SUCCESS.ToString();
            await _freeMintRepository.AddOrUpdateAsync(index);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "[FreeMint] error");
        }

        // how to handle transactinoInfo
        // success
        // save 
    }
}