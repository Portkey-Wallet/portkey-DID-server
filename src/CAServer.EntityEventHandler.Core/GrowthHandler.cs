using System;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using CAServer.Entities.Es;
using CAServer.Growth.Etos;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.ObjectMapping;

namespace CAServer.EntityEventHandler.Core;

public class GrowthHandler : IDistributedEventHandler<CreateGrowthEto>, ITransientDependency
{
    private readonly INESTRepository<GrowthIndex, string> _growthRepository;
    private readonly IObjectMapper _objectMapper;
    private readonly ILogger<GrowthHandler> _logger;

    public GrowthHandler(INESTRepository<GrowthIndex, string> growthRepository,
        IObjectMapper objectMapper,
        ILogger<GrowthHandler> logger)
    {
        _growthRepository = growthRepository;
        _objectMapper = objectMapper;
        _logger = logger;
    }

    public async Task HandleEventAsync(CreateGrowthEto eventData)
    {
        try
        {
            _ =  _growthRepository.AddAsync(_objectMapper.Map<CreateGrowthEto, GrowthIndex>(eventData));
            _logger.LogInformation(
                "growth info add success, id:{id}, userId:{userId}, caHash:{caHash}, inviteCode:{inviteCode}, referralCode:{referralCode}, projectCode:{projectCode}, shortLinkCode{shortLinkCode}",
                eventData.Id, eventData.UserId, eventData.CaHash, eventData.InviteCode ?? string.Empty,
                eventData.ReferralCode ?? string.Empty, eventData.ProjectCode ?? string.Empty, eventData.ShortLinkCode);
        }
        catch (Exception e)
        {
            _logger.LogError(e,
                "growth info add error, id:{id}, userId:{userId}, caHash:{caHash}, inviteCode:{inviteCode}, referralCode:{referralCode}, projectCode:{projectCode}, shortLinkCode{shortLinkCode}",
                eventData.Id, eventData.UserId, eventData.CaHash, eventData.InviteCode ?? string.Empty,
                eventData.ReferralCode ?? string.Empty, eventData.ProjectCode ?? string.Empty, eventData.ShortLinkCode);
        }
    }
}