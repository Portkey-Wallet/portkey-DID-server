using System.Threading.Tasks;
using CAServer.FreeMint.Dtos;

namespace CAServer.FreeMint;

public interface IFreeMintAppService
{
    Task<GetRecentStatusDto> GetRecentStatusAsync();
    Task<GetMintInfoDto> GetMintInfoAsync();
    Task<ConfirmDto> ConfirmAsync(ConfirmRequestDto requestDto);
    Task<ConfirmDto> MintAgainAsync(MintAgainRequestDto requestDto);
    Task<GetStatusDto> GetStatusAsync(string itemId);
    Task<GetNftItemDetailDto> GetNftItemDetailAsync(string itemId);
}