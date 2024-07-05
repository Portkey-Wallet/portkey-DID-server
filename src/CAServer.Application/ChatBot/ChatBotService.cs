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
public class ChatBotService : CAServerAppService, IChatBotAppService
{
    private readonly ICacheProvider _cacheProvider;
    private const string InitChatBotContactTimesCacheKey = "Portkey:InitChatBotContactTimes";
    private readonly IContactProvider _contactProvider;
    private readonly ChatBotOptions _chatBotOptions;
    private readonly IContactAppService _contactAppService;
    private readonly INESTRepository<ContactIndex, Guid> _contactIndexRepository;
    private readonly ILogger<ChatBotService> _logger;


    public ChatBotService(ICacheProvider cacheProvider, IContactProvider contactProvider,
        IOptionsSnapshot<ChatBotOptions> chatBotOptions, IContactAppService contactAppService,
        INESTRepository<ContactIndex, Guid> contactIndexRepository, ILogger<ChatBotService> logger)
    {
        _cacheProvider = cacheProvider;
        _contactProvider = contactProvider;
        _contactAppService = contactAppService;
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
            Id = GuidGenerator.Create(),
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
                index.UserId = holder.UserId;
                await _contactIndexRepository.AddAsync(index);
                _logger.LogDebug("Add ChatBot to ES,entity is {entity}", JsonConvert.SerializeObject(index));
            }

            skip += limit;
        }
    }
}