using System;
using System.Collections.Generic;
using CAServer.Commons.Etos;

namespace CAServer.Transfer.Dtos;

public class OrderIndexDto
{
    public Guid Id { get; set; }
    public string OrderType { get; set; }
    public string Status { get; set; }
    public long LastModifyTime { get; set; }
    public long ArrivalTime { get; set; }
    public TransferInfoDto FromTransfer { get; set; }
    public TransferInfoDto ToTransfer { get; set; }
}

public class TransferInfoDto : ChainDisplayNameDto
{
    public string Network { get; set; }
    public string Symbol { get; set; }
    public string Amount { get; set; }
    public string FromAddress { get; set; }
    public string ToAddress { get; set; }
    public List<FeeInfo> FeeInfo { get; set; } = new();
}

public class FeeInfo
{
    public string? Name { get; set; }
    public string Symbol { get; set; }
    public string Amount { get; set; }
    public string Decimals { get; set; }


    public FeeInfo()
    {
        
    }
    
    public FeeInfo(string symbol, string amount, string? name = null)
    {
        Symbol = symbol;
        Amount = amount;
        Name = name;
    }
    
    public FeeInfo(string symbol, string amount, string decimals, string? name = null)
    {
        Symbol = symbol;
        Amount = amount;
        Decimals = decimals;
        Name = name;
    }
    
    public class FeeName
    {
        public const string NetworkFee = "NetworkFee";
        public const string CoBoFee = "CoBoFee";
    }
    
}