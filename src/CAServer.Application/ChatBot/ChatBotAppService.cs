using System;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using CAServer.Cache;
using CAServer.Contacts;
using CAServer.Contacts.Provider;
using CAServer.Entities.Es;
using CAServer.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Volo.Abp;
using Volo.Abp.Auditing;
using ImInfo = CAServer.Entities.Es.ImInfo;

namespace CAServer.ChatBot;

[RemoteService(false)]
[DisableAuditing]
public class ChatBotAppService : CAServerAppService, IChatBotAppService
{
    private readonly ICacheProvider _cacheProvider;
    private const string InitChatBotContactTimesCacheKey = "Portkey:InitChatBotContactTimes";
    private readonly IContactProvider _contactProvider;
    private readonly ChatBotOptions _chatBotOptions;
    private readonly INESTRepository<ContactIndex, Guid> _contactIndexRepository;
    private readonly ILogger<ChatBotAppService> _logger;


    public ChatBotAppService(ICacheProvider cacheProvider, IContactProvider contactProvider,
        IOptionsSnapshot<ChatBotOptions> chatBotOptions,
        INESTRepository<ContactIndex, Guid> contactIndexRepository, ILogger<ChatBotAppService> logger)
    {
        _cacheProvider = cacheProvider;
        _contactProvider = contactProvider;
        _contactIndexRepository = contactIndexRepository;
        _logger = logger;
        _chatBotOptions = chatBotOptions.Value;
    }

    public async Task InitAddChatBotContactAsync()
    {
        var initTimes = await _cacheProvider.Get(InitChatBotContactTimesCacheKey);
        if (initTimes.HasValue)
        {
            _logger.LogDebug("Add ChatBot has been finished.");
            return;
        }

        var skip = 0;
        var limit = 100;
        //var userInfo = await _contactAppService.GetImInfoAsync(_chatBotOptions.RelationId);
        var index = new ContactIndex
        {
            Index = "K",
            Name = "",
            Avatar = _chatBotOptions.Avatar,
            ImInfo = new ImInfo
            {
                RelationId = _chatBotOptions.RelationId,
                PortkeyId = _chatBotOptions.PortkeyId,
                Name = _chatBotOptions.Name
            },
            IsDeleted = false,
            CreateTime = DateTime.UtcNow,
            ModificationTime = DateTime.UtcNow,
            ContactType = 1
        };

        while (true)
        {
            var result = await _contactProvider.GetAllCaHolderAsync(skip, limit);
            if (result.Count == 0)
            {
                break;
            }

            foreach (var holder in result)
            {
                var chatBot =
                    await _contactProvider.GetContactByRelationIdAsync(holder.UserId, _chatBotOptions.RelationId);
                if (chatBot != null)
                {
                    index.UserId = holder.UserId;
                    index.Id = chatBot.Id;
                    await _contactIndexRepository.AddOrUpdateAsync(index);
                    continue;
                }

                index.Id = Guid.NewGuid();
                index.UserId = holder.UserId;
                await _contactIndexRepository.AddAsync(index);
                _logger.LogDebug("Add ChatBot to ES,entity is {entity}", JsonConvert.SerializeObject(index));
            }

            skip += limit;
        }

        var expire = TimeSpan.FromDays(1);
        await _cacheProvider.Set(InitChatBotContactTimesCacheKey, "Init", expire);
    }
}