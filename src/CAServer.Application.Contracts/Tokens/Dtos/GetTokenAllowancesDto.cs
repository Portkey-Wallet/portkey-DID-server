using System.Collections.Generic;

namespace CAServer.Tokens.Dtos;

public class GetTokenAllowancesDto
{
    public List<TokenAllowance> Data { get; set; } = new();
    public long TotalRecordCount { get; set; }
}

public class TokenAllowance
{
    public string ChainId { get; set; }
    public string ContractAddress { get; set; }
    public string Url { get; set; }
    public string Icon { get; set; }
    public string Name { get; set; }
    public long Allowance { get; set; }
    public List<SymbolApprove> SymbolApproveList { get; set; } = new();
}

public class SymbolApprove
{
    public string Symbol { get; set; }
    public long Amount { get; set; }
    public int Decimals { get; set; }
}