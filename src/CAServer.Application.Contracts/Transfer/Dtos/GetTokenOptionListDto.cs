using System.Collections.Generic;

namespace CAServer.Transfer.Dtos;

public class GetTokenOptionListDto
{
    public List<TokenOptionConfigDto> TokenList { get; set; }
}

public class TokenOptionConfigDto
{
    public string Name { get; set; }
    public string Symbol { get; set; }
    public int Decimals { get; set; }
    public string Icon { get; set; }
    public string ContractAddress { get; set; }
    public List<ToTokenOptionConfigDto> ToTokenList { get; set; }
}

public class ToTokenOptionConfigDto
{
    public string Name { get; set; }
    public string Symbol { get; set; }
    public List<string> ChainIdList { get; set; }
    public int Decimals { get; set; }
    public string Icon { get; set; }
}