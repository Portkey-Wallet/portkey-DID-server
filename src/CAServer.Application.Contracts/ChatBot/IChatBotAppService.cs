using System.Threading.Tasks;

namespace CAServer.ChatBot;

public interface IChatBotAppService
{
    Task InitAddChatBotContactAsync();
}