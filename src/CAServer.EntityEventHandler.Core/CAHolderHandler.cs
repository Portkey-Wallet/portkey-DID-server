using System;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using CAServer.Contacts.Provider;
using CAServer.Entities.Es;
using CAServer.Etos;
using CAServer.Grains.Grain.Contacts;
using CAServer.Tokens;
using CAServer.Tokens.Dtos;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Orleans;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.ObjectMapping;

namespace CAServer.EntityEventHandler.Core;

public class CAHolderHandler : IDistributedEventHandler<CreateUserEto>,
    IDistributedEventHandler<UpdateCAHolderEto>,
    IDistributedEventHandler<DeleteCAHolderEto>
    , ITransientDependency
{
    private readonly INESTRepository<CAHolderIndex, Guid> _caHolderRepository;
    private readonly IObjectMapper _objectMapper;
    private readonly ILogger<CAHolderHandler> _logger;
    private readonly IClusterClient _clusterClient;
    private readonly IUserTokenAppService _userTokenAppService;
    private readonly IContactProvider _contactProvider;
    private readonly INESTRepository<ContactIndex, Guid> _contactRepository;

    public CAHolderHandler(INESTRepository<CAHolderIndex, Guid> caHolderRepository,
        IObjectMapper objectMapper,
        ILogger<CAHolderHandler> logger,
        IClusterClient clusterClient,
        IUserTokenAppService userTokenAppService,
        IContactProvider contactProvider,
        INESTRepository<ContactIndex, Guid> contactRepository)
    {
        _caHolderRepository = caHolderRepository;
        _objectMapper = objectMapper;
        _logger = logger;
        _clusterClient = clusterClient;
        _userTokenAppService = userTokenAppService;
        _contactProvider = contactProvider;
        _contactRepository = contactRepository;
    }

    public async Task HandleEventAsync(CreateUserEto eventData)
    {
        try
        {
            eventData.Nickname = "Wallet 01";
            _logger.LogInformation("receive create token event...");
            var grain = _clusterClient.GetGrain<ICAHolderGrain>(eventData.UserId);
            var result = await grain.AddHolderAsync(_objectMapper.Map<CreateUserEto, CAHolderGrainDto>(eventData));

            if (!result.Success)
            {
                _logger.LogError("{Message}", JsonConvert.SerializeObject(result));
                return;
            }

            await _caHolderRepository.AddAsync(_objectMapper.Map<CAHolderGrainDto, CAHolderIndex>(result.Data));

            _logger.LogInformation("add user token...");
            await _userTokenAppService.AddUserTokenAsync(eventData.UserId, new AddUserTokenInput());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Message}: {Data}", "Create CA holder fail", JsonConvert.SerializeObject(eventData));
        }
    }

    public async Task HandleEventAsync(UpdateCAHolderEto eventData)
    {
        try
        {
            await _caHolderRepository.UpdateAsync(_objectMapper.Map<UpdateCAHolderEto, CAHolderIndex>(eventData));
            _logger.LogInformation("caHolder wallet name update success, id: {id}", eventData.Id);

            var contacts = await _contactProvider.GetAddedContactsAsync(eventData.UserId);
            if (contacts == null || contacts.Count == 0) return;

            foreach (var contact in contacts)
            {
                if (contact.CaHolderInfo == null) return;
                var grain = _clusterClient.GetGrain<IContactGrain>(contact.Id);
                var updateResult = await grain.UpdateWalletName(eventData.Nickname);

                if (!updateResult.Success)
                {
                    _logger.LogWarning("contact wallet name update fail, contactId: {id}, message:{message}",
                        contact.Id, updateResult.Message);
                    break;
                }

                contact.CaHolderInfo.WalletName = eventData.Nickname;
                contact.ModificationTime = DateTime.UtcNow;
                contact.Index = updateResult.Data.Index;
                
                await _contactRepository.UpdateAsync(contact);
                _logger.LogInformation("contact wallet name update success, contactId: {id}", contact.Id);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "update nick name error, id:{id}, nickName:{nickName}, userId:{userId}",
                eventData.Id.ToString(), eventData.Nickname, eventData.UserId.ToString());
        }
    }

    public async Task HandleEventAsync(DeleteCAHolderEto eventData)
    {
        try
        {
            await _caHolderRepository.UpdateAsync(_objectMapper.Map<DeleteCAHolderEto, CAHolderIndex>(eventData));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Delete holder error, userId: {userId}", eventData.UserId.ToString());
        }
    }
}