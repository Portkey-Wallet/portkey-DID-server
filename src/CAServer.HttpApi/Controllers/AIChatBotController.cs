using System.Threading.Tasks;
using CAServer.ChatBot;
using CAServer.ChatBot.Dtos;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;

namespace CAServer.Controllers;

[RemoteService]
[Area("app")]
[ControllerName("ChatBot")]
[Route("api/app/chatBot/")]
public class AIChatBotController
{

    private readonly IChatBotAppService _chatBotAppService;

    public AIChatBotController(IChatBotAppService chatBotAppService)
    {
        _chatBotAppService = chatBotAppService;
    }

    [HttpGet("enable")]
    public async Task<string> EnableChatBot(ChatBotEnableDto input)
    {
        return await _chatBotAppService.EnableChatBotAsync(input);
    }
    
    [HttpGet("disable")]
    public async Task<string> DisableChatBot(ChatBotEnableDto input)
    {
        return await _chatBotAppService.DisableChatBotAsync(input);
    }
}