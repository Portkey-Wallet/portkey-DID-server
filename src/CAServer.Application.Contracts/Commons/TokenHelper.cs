using System.Collections.Generic;
using System.Linq;
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
            TotalBalanceInUsd =  tokenDto.TotalBalanceInUsd,
            Data = new List<TokenWithoutChain>()
        };

        var tokenMap = tokenDto.Data
            .GroupBy(t => t.Symbol)
            .Select(g => new
            {
                Symbol = g.Key,
                Balance = g.Sum(t => decimal.Parse(t.Balance)),
                BalanceInUsd = g.Sum(t => decimal.Parse(t.BalanceInUsd))
            });
        TokenWithoutChain tokenWithoutChain = new TokenWithoutChain();
        foreach (var token in tokenDto.Data)
        {
            if (!token.Symbol.Equals(tokenWithoutChain.Symbol))
            {
                tokenWithoutChain = new TokenWithoutChain
                {
                    Symbol = token.Symbol,
                    Price = token.Price,
                    Balance = tokenMap.FirstOrDefault(g => g.Symbol == token.Symbol).Balance.ToString(),
                    Decimals = token.Decimals,
                    BalanceInUsd = tokenMap.FirstOrDefault(g => g.Symbol == token.Symbol).BalanceInUsd.ToString(),
                    TokenContractAddress = token.TokenContractAddress,
                    ImageUrl = token.ImageUrl,
                    Label = token.Label,
                    tokens = new List<Token> { token }
                };
                result.Data.Add(tokenWithoutChain);
            }
            else
            {
                tokenWithoutChain.tokens.Add(token);
            }
        }
        
        result.TotalRecordCount = result.Data.Count();
        return result;
    }
}