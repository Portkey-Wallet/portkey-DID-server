using System.Collections.Generic;

namespace CAServer.Transfer.Dtos;

public class EBridgeLimiterDto
{
    public List<LimiterDto> Items { get; set; }
}

public class LimiterDto
{
    public string FromChain { get; set; }
    public string ToChain { get; set; }
    public List<RateLimitInfoDto> ReceiptRateLimitsInfo { get; set; }
    public List<RateLimitInfoDto> SwapRateLimitsInfo { get; set; }
}

public class RateLimitInfoDto
{
    public string Token { get; set; }
    public double Capacity { get; set; }
    public double RefillRate { get; set; }
    public int MaximumTimeConsumed { get; set; }
}