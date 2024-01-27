using CAServer.Grains.State.Tokens;
using CAServer.Tokens.Dtos;
using Orleans;
using Volo.Abp.ObjectMapping;

namespace CAServer.Grains.Grain.Tokens.UserTokens;

public class UserTokenGrain : Grain<UserTokenState>, IUserTokenGrain
{
    private readonly IObjectMapper _objectMapper;

    public UserTokenGrain(IObjectMapper objectMapper)
    {
        _objectMapper = objectMapper;
    }

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

    public Task<GrainResultDto<UserTokenGrainDto>> GetUserToken()
    {
        var result = new GrainResultDto<UserTokenGrainDto>();
        if (State.IsDelete || State.Id == Guid.Empty)
        {
            result.Message = UserTokenMessage.NotExistMessage;
            return Task.FromResult(result);
        }

        result.Success = true;
        result.Data = _objectMapper.Map<UserTokenState, UserTokenGrainDto>(State);
        return Task.FromResult(result);
    }

    public async Task<GrainResultDto<UserTokenGrainDto>> AddUserTokenAsync(Guid userId, UserTokenGrainDto tokenItem)
    {
        var result = new GrainResultDto<UserTokenGrainDto>();
        var userTokenSymbolGrain = GetUserTokenSymbolGrain(userId, tokenItem.Token.ChainId, tokenItem.Token.Symbol);
        var toAdd = await userTokenSymbolGrain.AddUserTokenSymbolAsync(userId, tokenItem.Token.ChainId,
            tokenItem.Token.Symbol);
        if (!toAdd)
        {
            result.Message = UserTokenMessage.ExistedMessage;
            return result;
        }

        if (State.IsDelete)
        {
            State.IsDelete = false;
        }
        else
        {
            State.Id = this.GetPrimaryKey();
            State.UserId = userId;
            State.IsDefault = tokenItem.IsDefault;
            State.IsDisplay = tokenItem.IsDisplay;
            State.SortWeight = tokenItem.SortWeight;
            State.Token = new Token
            {
                Id = Guid.NewGuid(),
                Address = tokenItem.Token.Address,
                ChainId = tokenItem.Token.ChainId,
                Decimals = tokenItem.Token.Decimals,
                Symbol = tokenItem.Token.Symbol
            };
        }

        await WriteStateAsync();
        result.Success = true;
        result.Data = _objectMapper.Map<UserTokenState, UserTokenGrainDto>(State);
        return result;
    }

    public async Task<GrainResultDto<UserTokenGrainDto>> ChangeTokenDisplayAsync(Guid userId, bool isDisplay,
        bool isDelete = false)
    {
        var result = new GrainResultDto<UserTokenGrainDto>();
        if (userId != State.UserId)
        {
            result.Message = UserTokenMessage.UserNotMatchMessage;
            return result;
        }

        if (State.Token.Symbol == "ELF")
        {
            result.Message = UserTokenMessage.SymbolCanNotChangeMessage;
            return result;
        }

        if (isDelete)
        {
            var userTokenSymbolGrain = GetUserTokenSymbolGrain(userId, State.Token.ChainId, State.Token.Symbol);
            await userTokenSymbolGrain.DeleteUserTokenSymbol();
        }

        State.IsDisplay = isDisplay;
        State.IsDelete = isDelete;
        await WriteStateAsync();
        result.Success = true;
        result.Data = _objectMapper.Map<UserTokenState, UserTokenGrainDto>(State);
        return result;
    }

    private IUserTokenSymbolGrain GetUserTokenSymbolGrain(Guid userId, string chainId, string symbol)
    {
        return GrainFactory.GetGrain<IUserTokenSymbolGrain>(
            GrainIdHelper.GenerateGrainId(userId.ToString("N"), chainId, symbol));
    }

    public async Task<GrainResultDto> ModifySortWeight()
    {
        var result = new GrainResultDto();
        if (State.Id == Guid.Empty)
        {
            result.Message = UserTokenMessage.NotExistMessage;
            return result;
        }

        if (State.Token != null && State.Token.Symbol != "ELF")
        {
            State.SortWeight = 0;
            await WriteStateAsync();
        }

        result.Success = true;
        return result;
    }
}