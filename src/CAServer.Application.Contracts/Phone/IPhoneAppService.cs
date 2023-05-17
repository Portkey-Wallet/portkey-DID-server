using System.Collections.Generic;
using System.Threading.Tasks;
using CAServer.Phone.Dtos;
using JetBrains.Annotations;

namespace CAServer.Phone;

public interface IPhoneAppService
{
    Task<PhoneInfoListDto> GetPhoneInfoAsync();
}