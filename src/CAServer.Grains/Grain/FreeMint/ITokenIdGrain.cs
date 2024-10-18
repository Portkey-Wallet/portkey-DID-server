using CAServer.Commons;
using CAServer.Grains.State.FreeMint;
using Orleans;
using Volo.Abp;

namespace CAServer.Grains.Grain.FreeMint;

public interface ITokenIdGrain : IGrainWithStringKey
{
    Task<string> GenerateTokenId();
    Task<GrainResultDto> CheckUseTokenId(string tokenId);
    Task<GrainResultDto> UseTokenId(string tokenId);
}

public class TokenIdGrain : Grain<TokenIdState>, ITokenIdGrain
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

    public async Task<string> GenerateTokenId()
    {
        if (this.GetPrimaryKeyString() != CommonConstant.FreeMintTokenIdGrainId)
        {
            throw new UserFriendlyException("invalid grain id");
        }

        State.CurrentTokenId += 1;
        await WriteStateAsync();
        return State.CurrentTokenId.ToString();
    }

    public Task<GrainResultDto> CheckUseTokenId(string tokenId)
    {
        var result = new GrainResultDto();
        if (!int.TryParse(tokenId, out var tokenNum))
        {
            result.Message = "Invalid tokenId.";
        }

        if (State.UsedTokenIds.Contains(tokenNum))
        {
            result.Message = "TokenId already exists.";
        }

        result.Success = true;
        return Task.FromResult(result);
    }

    public async Task<GrainResultDto> UseTokenId(string tokenId)
    {
        var result = new GrainResultDto();
        var checkResult = CheckUseTokenId(tokenId).Result;
        if (!checkResult.Success)
        {
            result.Message = checkResult.Message;
        }

        State.UsedTokenIds.Add(int.Parse(tokenId));
        await WriteStateAsync();
        result.Success = true;
        return result;
    }
}