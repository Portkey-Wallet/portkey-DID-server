using System.Threading.Tasks;

namespace CAServer.AddressExtraInfo;

public interface IAddressExtraInfoService
{
    Task<string> GetLoinInAccount();
}