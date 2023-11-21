using System.Threading.Tasks;
using CAServer.Tokens.Dtos;

namespace CAServer.Tokens.Provider;

public interface IExchangeProvider
{
    public ExchangeProviderName Name();

    public Task<TokenExchange> Latest(string fromSymbol, string toSymbol);

    public Task<TokenExchange> History(string fromSymbol, string toSymbol, long timestamp);

}


public enum ExchangeProviderName
{
    Binance,
    Okx,
}