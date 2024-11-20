using System.Collections.Generic;
using CAServer.UserAssets.Dtos;

namespace CAServer.Awaken;

public class AwakenSupportedTokenResponse
{
    public long Total { get; set; }
    public List<Token> Data { get; set; }
}