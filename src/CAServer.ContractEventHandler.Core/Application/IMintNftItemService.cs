using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using CAServer.Entities.Es;
using CAServer.FreeMint.Dtos;
using CAServer.FreeMint.Etos;
using Microsoft.Extensions.Logging;
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

    public MintNftItemService(INESTRepository<FreeMintIndex, string> freeMintRepository,
        ILogger<MintNftItemService> logger, IObjectMapper objectMapper)
    {
        _freeMintRepository = freeMintRepository;
        _logger = logger;
        _objectMapper = objectMapper;
    }

    public async Task MintAsync(FreeMintEto eventData)
    {
        // save in es
        var index = await _freeMintRepository.GetAsync(eventData.ConfirmInfo.ItemId);
        if (index == null)
        {
            index = new FreeMintIndex();
            _objectMapper.Map(index, eventData.ConfirmInfo);
            index.CollectionInfo = _objectMapper.Map<FreeMintCollectionInfo, CollectionInfo>(eventData.CollectionInfo);
            await _freeMintRepository.AddOrUpdateAsync(index);
        }
        else
        {
            //index.
        }
        // send transaction
        
        // how to handle transactinoInfo
        throw new System.NotImplementedException();
    }
}