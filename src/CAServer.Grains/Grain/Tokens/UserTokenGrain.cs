using CAServer.Entities.Etos;
using CAServer.Grains.State;
using CAServer.Grains.State.Tokens;
using CAServer.Tokens;
using Orleans;
using Volo.Abp.EventBus.Distributed;

namespace CAServer.Grains.Grain.Tokens;

public class TokenGrain:Grain<UserTokenState>,IUserTokenGrain
{
    private readonly IDistributedEventBus _distributedEventBus;

    public TokenGrain(IDistributedEventBus distributedEventBus)
    {
        _distributedEventBus = distributedEventBus;
    }
    public override async Task OnActivateAsync()
    {
        Guid primaryKey = this.GetPrimaryKey();
        State.Id = primaryKey;
        await ReadStateAsync();
        await base.OnActivateAsync();
    }

    public override async Task OnDeactivateAsync()
    {
        await WriteStateAsync();
        await base.OnDeactivateAsync();
    }
    
    public async Task<UserToken> AddUserTokenAsync(Guid userId, Token tokenItem)
    {
        State.UserId = userId;
        State.IsDefault = false;
        State.IsDisplay = false;
        if (tokenItem.Symbol == "ELF")
        {
            State.SortWeight = 1;
        }
        State.Token = tokenItem;
        await WriteStateAsync();
        return new UserToken
        {
            Id = this.GetPrimaryKey(),
            IsDefault = State.IsDefault,
            IsDisplay = State.IsDisplay,
            UserId = State.UserId,
            SortWeight = State.SortWeight,
            Token = tokenItem
        };
    }

    public async Task ChangeTokenDisplayAsync(bool isDisplay)
    {
        await ReadStateAsync();
        State.IsDisplay = isDisplay;
        await WriteStateAsync();
        await _distributedEventBus.PublishAsync(new UserTokenEto
        {
            Id = this.GetPrimaryKey(),
            IsDefault = State.IsDefault,
            IsDisplay = State.IsDisplay,
            UserId = State.UserId,
            SortWeight = State.SortWeight,
            Token = State.Token
        });
    }
}