using System.Collections.Generic;
using System.Linq;
using CAServer.Commons;
using CAServer.UserAssets.Dtos;

namespace CAServer.Common;

public static class TokenHelper
{
    public static GetTokenV2Dto ConvertFromGetToken(GetTokenDto tokenDto)
    {
        if (null == tokenDto || tokenDto.Data?.Count == 0)
        {
            return new GetTokenV2Dto
            {
                TotalBalanceInUsd = "0",
                TotalRecordCount = 0,
                Data = new List<TokenWithoutChain>()
            };
        }

        GetTokenV2Dto result = new GetTokenV2Dto
        {
            TotalBalanceInUsd = tokenDto.TotalBalanceInUsd,
            Data = new List<TokenWithoutChain>()
        };

        var tokenMap = tokenDto.Data
            .GroupBy(t => t.Symbol)
            .Select(g => new
            {
                Symbol = g.Key,
                Balance = g.Sum(t => ParseDecimal(t.Balance)),
                BalanceInUsd = g.Sum(t => ParseDecimal(t.BalanceInUsd))
            });
        Dictionary<string, TokenWithoutChain> generatedTokens = new Dictionary<string, TokenWithoutChain>();
        foreach (var token in tokenDto.Data)
        {
            ChainDisplayNameHelper.SetDisplayName(token);
            token.BalanceInUsd = ConvertTwoDecimal(token.BalanceInUsd);
            if (generatedTokens.ContainsKey(token.Symbol))
            {
                generatedTokens[token.Symbol].tokens.Add(token);
            }
            else
            {
                var balanceInUsd = tokenMap.FirstOrDefault(g => g.Symbol == token.Symbol).BalanceInUsd.ToString("F2");                TokenWithoutChain tokenWithoutChain = new TokenWithoutChain
                {
                    Symbol = token.Symbol,
                    Price = token.Price,
                    Balance = tokenMap.FirstOrDefault(g => g.Symbol == token.Symbol).Balance.ToString(),
                    Decimals = token.Decimals,
                    BalanceInUsd = "".Equals(token.BalanceInUsd) ? "" : balanceInUsd,
                    TokenContractAddress = token.TokenContractAddress,
                    ImageUrl = token.ImageUrl,
                    Label = token.Label,
                    tokens = new List<Token> { token }
                };
                result.Data.Add(tokenWithoutChain);
                generatedTokens.Add(token.Symbol, tokenWithoutChain);
            }
        }

        foreach (var tokenWithoutChain in generatedTokens.Where(tokenWithoutChain => tokenWithoutChain.Value?.tokens?.Count == 2))
        {
            if (tokenWithoutChain.Value.tokens[0].ChainId != CommonConstant.MainChainId) continue;
            (tokenWithoutChain.Value.tokens[0], tokenWithoutChain.Value.tokens[1]) = (tokenWithoutChain.Value.tokens[1], tokenWithoutChain.Value.tokens[0]);
        }

        result.TotalRecordCount = result.Data.Count();
        result.TotalDisplayCount = result.Data.Select(item => item.tokens.Count).Sum();
        result.TotalRecordCount = result.Data.Count;
        return result;
    }

    private static decimal ParseDecimal(string number)
    {
        decimal result;
        return decimal.TryParse(number, out result) ? result : 0;
    }
    
    private static string ConvertTwoDecimal(string number)
    {
        var decimalValue = ParseDecimal(number);
        if (number.Contains('.'))
        {
            return ((decimal)0.01).CompareTo(decimalValue) > 0 ? decimalValue.ToString("F4") : decimalValue.ToString("F2");
        }
        return number;
    }
}