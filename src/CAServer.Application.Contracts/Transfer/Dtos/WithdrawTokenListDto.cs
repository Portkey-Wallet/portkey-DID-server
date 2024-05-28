using System.Collections.Generic;

namespace CAServer.Transfer.Dtos;

public class WithdrawTokenListDto
{
    public List<TokenConfigDto> TokenList { get; set; }

    public string ChainId { get; set; }
}

public class TokenConfigDto
{
    public string Name { get; set; }
    public string Symbol { get; set; }
    public int Decimals { get; set; }
    public string Icon { get; set; }
    public string ContractAddress { get; set; }
}