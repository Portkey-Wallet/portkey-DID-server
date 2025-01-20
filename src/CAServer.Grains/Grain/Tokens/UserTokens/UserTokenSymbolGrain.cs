using CAServer.Grains.State.Tokens;

namespace CAServer.Grains.Grain.Tokens.UserTokens;

public class UserTokenSymbolGrain : Grain<UserTokenSymbolState>, IUserTokenSymbolGrain
{
    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        await ReadStateAsync();
        await base.OnActivateAsync(cancellationToken);
    }

    public override async Task OnDeactivateAsync(DeactivationReason reason, CancellationToken token)
    {
        await WriteStateAsync();
        await base.OnDeactivateAsync(reason, token);
    }

    public async Task<bool> AddUserTokenSymbolAsync(Guid userId, string chainId, string symbol)
    {
        if (State.IsDelete)
        {
            State.IsDelete = false;
            await WriteStateAsync();
            return true;
        }

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

    public async Task DeleteUserTokenSymbol()
    {
        if(State.IsDelete) return;
        
        State.IsDelete = true;
        await WriteStateAsync();
    }

    private bool IsUserTokenSymbolAvailableAsync()
    {
        return State.UserId == Guid.Empty && string.IsNullOrWhiteSpace(State.ChainId) &&
               string.IsNullOrWhiteSpace(State.Symbol);
    }

    public Task<bool> IsUserTokenSymbolExistAsync(string chainId, string symbol)
    {
        return Task.FromResult(State.ChainId == chainId && State.Symbol == symbol && !State.IsDelete);
    }
}