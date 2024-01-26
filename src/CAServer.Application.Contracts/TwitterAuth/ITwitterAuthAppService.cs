using System.Threading.Tasks;
using CAServer.TwitterAuth.Dtos;

namespace CAServer.TwitterAuth;

public interface ITwitterAuthAppService
{
    Task ReceiveAsync(TwitterAuthDto appleAuthDto);
}