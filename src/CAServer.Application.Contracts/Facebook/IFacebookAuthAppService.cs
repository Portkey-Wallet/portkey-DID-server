using System.Threading.Tasks;
using CAServer.Facebook.Dtos;
using CAServer.Verifier;

namespace CAServer.Facebook;

public interface IFacebookAuthAppService
{
    Task<FacebookAuthResponseDto> ReceiveAsync(string code, ApplicationType applicationType);
}