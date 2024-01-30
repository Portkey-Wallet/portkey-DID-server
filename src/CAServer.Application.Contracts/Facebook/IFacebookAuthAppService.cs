using System.Threading.Tasks;
using CAServer.Facebook.Dtos;

namespace CAServer.Facebook;

public interface IFacebookAuthAppService
{
    Task<FacebookAuthResponse> ReceiveAsync(FacebookAuthDto authDto);
}