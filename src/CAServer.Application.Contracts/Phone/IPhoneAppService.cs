using System.Collections.Generic;
using System.Threading.Tasks;
using CAServer.Phone.Dtos;

namespace CAServer.Phone;

public interface IPhoneAppService
{
    Task<PhoneInfoListDto> GetPhoneInfo();
}