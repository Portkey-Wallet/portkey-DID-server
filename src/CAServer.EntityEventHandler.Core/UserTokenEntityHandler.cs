using System;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using CAServer.Entities.Es;
using CAServer.Entities.Etos;
using Microsoft.Extensions.Logging;
using Volo.Abp.EventBus.Distributed;

namespace CAServer.EntityEventHandler.Core;

public class UserTokenEntityHandler : EntityHandlerBase,
    IDistributedEventHandler<UserTokenEto>
{
    private readonly INESTRepository<UserTokenIndex,Guid> _userTokenIndexRepository;

    public UserTokenEntityHandler(INESTRepository<UserTokenIndex, Guid> userTokenIndexRepository)
    {
        _userTokenIndexRepository = userTokenIndexRepository;
    }

    public async Task HandleEventAsync(UserTokenEto eventData)
    {
        var index = ObjectMapper.Map<UserTokenEto, UserTokenIndex>(eventData);
        await _userTokenIndexRepository.AddOrUpdateAsync(index);
    }
}