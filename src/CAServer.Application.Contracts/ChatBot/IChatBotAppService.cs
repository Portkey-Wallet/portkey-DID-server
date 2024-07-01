using System.Threading.Tasks;
using CAServer.ChatBot.Dtos;

namespace CAServer.ChatBot;

public interface IChatBotAppService
{
    Task<string> EnableChatBotAsync(ChatBotEnableDto input);
    Task<string> DisableChatBotAsync(ChatBotEnableDto input);
}