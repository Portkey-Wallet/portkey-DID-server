using System;
using System.Threading.Tasks;

namespace CAServer.ThirdPart;

public interface ITransakServiceAppService
{
    
    // apikey -> accessToken
    public Task<Tuple<string, string>> GetAccessTokenAsync();
    
}