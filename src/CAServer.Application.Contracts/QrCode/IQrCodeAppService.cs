using System.Threading.Tasks;
using CAServer.QrCode.Dtos;

namespace CAServer.QrCode;

public interface IQrCodeAppService
{
    Task<bool> CreateAsync(QrCodeRequestDto input);
}