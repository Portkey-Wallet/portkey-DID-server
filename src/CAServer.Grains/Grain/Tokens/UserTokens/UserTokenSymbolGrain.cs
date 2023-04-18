using CAServer.Grains.State.Tokens;
using Orleans;

namespace CAServer.Grains.Grain.Tokens.UserTokens;

public class UserTokenSymbolGrain : Grain<UserTokenSymbolState>, IUserTokenSymbolGrain
{
    public override async Task OnActivateAsync()
    {
        await ReadStateAsync();
        await base.OnActivateAsync();
    }

    public override async Task OnDeactivateAsync()
    {
        await WriteStateAsync();
        await base.OnDeactivateAsync();
    }

    public async Task<bool> AddUserTokenSymbolAsync(Guid userId, string chainId, string symbol)
    {
        if (!IsUserTokenSymbolAvailableAsync())
        {
            return false;
        }
        State.UserId = userId;
        State.ChainId = chainId;
        State.Symbol = symbol;
        await WriteStateAsync();
        return true;
    }

    private bool IsUserTokenSymbolAvailableAsync()
    {
        return State.UserId == Guid.Empty && string.IsNullOrWhiteSpace(State.ChainId) &&
               string.IsNullOrWhiteSpace(State.Symbol);
    }

    public Task<bool> IsUserTokenSymbolExistAsync(string chainId, string symbol)
    {
        return Task.FromResult(State.ChainId == chainId && State.Symbol == symbol);
    }
}