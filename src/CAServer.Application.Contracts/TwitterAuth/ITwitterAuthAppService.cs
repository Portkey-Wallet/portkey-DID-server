using System.Threading.Tasks;
using CAServer.TwitterAuth.Dtos;

namespace CAServer.TwitterAuth;

public interface ITwitterAuthAppService
{
    Task<TwitterAuthResultDto> ReceiveAsync(TwitterAuthDto appleAuthDto);
    Task<string> LoginAsync();

    Task<TwitterAuthResultDto> LoginCallBackAsync(string oauthToken, string oauthVerifier);
}