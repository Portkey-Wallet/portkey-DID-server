using System.Threading.Tasks;
using CAServer.FreeMint.Dtos;

namespace CAServer.FreeMint;

public interface IFreeMintAppService
{
    Task<GetRecentStatusDto> GetRecentStatusAsync();
    Task<GetMintInfoDto> GetMintInfoAsync();
    Task<ConfirmDto> ConfirmAsync(ConfirmRequestDto requestDto);
    Task<GetStatusDto> GetStatusAsync(string itemId);
    Task<GetItemInfoDto> GetItemInfoAsync(string itemId);
}