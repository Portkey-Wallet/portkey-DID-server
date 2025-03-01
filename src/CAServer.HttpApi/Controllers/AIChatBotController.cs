using System.Threading.Tasks;
using Asp.Versioning;
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

    

    
    
}