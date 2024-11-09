using CAServer.Commons;
using CAServer.EnumType;
using CAServer.FreeMint.Dtos;
using CAServer.Grains.State.FreeMint;
using Microsoft.Extensions.Options;
using Orleans;
using Volo.Abp.ObjectMapping;

namespace CAServer.Grains.Grain.FreeMint;

public class FreeMintGrain : Grain<FreeMintState>, IFreeMintGrain
{
    private readonly IObjectMapper _objectMapper;
    private readonly FreeMintGrainOptions _freeMintOptions;

    public FreeMintGrain(IObjectMapper objectMapper, IOptions<FreeMintGrainOptions> freeMintOptions)
    {
        _objectMapper = objectMapper;
        _freeMintOptions = freeMintOptions.Value;
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

    public Task<GrainResultDto<FreeMintGrainDto>> GetFreeMintInfo()
    {
        var result = new GrainResultDto<FreeMintGrainDto>();
        if (string.IsNullOrEmpty(State.Id))
        {
            result.Message = "Free mint info not exists.";
            return Task.FromResult(result);
        }

        result.Success = true;
        result.Data = _objectMapper.Map<FreeMintState, FreeMintGrainDto>(State);
        return Task.FromResult(result);
    }

    public Task<GrainResultDto<GetRecentStatusDto>> GetRecentStatus()
    {
        var result = new GrainResultDto<GetRecentStatusDto>();
        var statusDto = new GetRecentStatusDto
        {
            Status = FreeMintStatus.NONE.ToString()
        };

        if (IsLimitExceed())
        {
            return Task.FromResult(new GrainResultDto<GetRecentStatusDto>(new GetRecentStatusDto
            {
                Status = FreeMintStatus.LimitExceed.ToString()
            }));
        }

        if (!string.IsNullOrEmpty(State.Id) && !State.MintInfos.IsNullOrEmpty())
        {
            var mintInfo = State.MintInfos.Last();
            statusDto.Status = mintInfo.Status.ToString();
            statusDto.ItemId = mintInfo.ItemId;
            statusDto.ImageUrl = mintInfo.ImageUrl;
        }

        result.Success = true;
        result.Data = statusDto;
        return Task.FromResult(result);
    }

    public Task<GrainResultDto<GetRecentStatusDto>> GetMintStatus(string itemId)
    {
        var result = new GrainResultDto<GetRecentStatusDto>();
        var itemInfo = State.MintInfos.FirstOrDefault(t => t.ItemId == itemId);

        if (string.IsNullOrEmpty(State.Id) || itemInfo == null)
        {
            result.Message = "Mint Nft Item info not exist.";
            return Task.FromResult(result);
        }

        result.Success = true;
        result.Data = new GetRecentStatusDto()
        {
            Status = itemInfo.Status.ToString(),
            ItemId = itemInfo.ItemId
        };

        return Task.FromResult(result);
    }

    public async Task<GrainResultDto<ItemMintInfo>> SaveMintInfo(MintNftDto mintNftDto)
    {
        var result = new GrainResultDto<ItemMintInfo>();

        State.Id = this.GetPrimaryKey().ToString();
        State.UserId = this.GetPrimaryKey();
        State.CollectionInfo = mintNftDto.CollectionInfo;

        if (IsLimitExceed())
        {
            result.Message = "Free mint limit exceed.";
            return result;
        }

        if (!State.PendingTokenId.IsNullOrEmpty())
        {
            result.Message = "Exists pending mint.";
            return result;
        }

        var mintInfo = State.MintInfos.FirstOrDefault(t => t.ItemId == mintNftDto.ConfirmInfo.ItemId);
        // exists and mint failed
        if (mintInfo != null && mintInfo.Status != FreeMintStatus.FAIL)
        {
            result.Message = $"Current item status: {mintInfo.Status.ToString()}.";
            return result;
        }

        // failed, try again
        if (mintInfo != null)
        {
            mintInfo.Name = mintNftDto.ConfirmInfo.Name;
            mintInfo.ImageUrl = mintNftDto.ConfirmInfo.ImageUrl;
            mintInfo.Description = mintNftDto.ConfirmInfo.Description;
            mintInfo.Status = FreeMintStatus.PENDING;

            SetInfo(mintInfo.TokenId, mintInfo.ItemId);
            await WriteStateAsync();
            return new GrainResultDto<ItemMintInfo>(mintInfo);
        }

        var tokenId = await GetTokenId();
        var nftInfo = new ItemMintInfo()
        {
            ItemId = Guid.NewGuid().ToString(),
            TokenId = tokenId,
            Name = mintNftDto.ConfirmInfo.Name,
            ImageUrl = mintNftDto.ConfirmInfo.ImageUrl,
            Description = mintNftDto.ConfirmInfo.Description,
            Status = FreeMintStatus.PENDING
        };

        SetInfo(tokenId, nftInfo.ItemId);
        State.MintInfos.Add(nftInfo);

        await WriteStateAsync();
        return new GrainResultDto<ItemMintInfo>(nftInfo);
    }

    private void SetInfo(string tokenId, string itemId)
    {
        State.PendingTokenId = tokenId;
        var date = GetDateKey();
        if (State.DateMintInfo.ContainsKey(date))
        {
            State.DateMintInfo[date].Add(itemId);
        }
        else
        {
            State.DateMintInfo.Add(date, new List<string> { itemId });
        }
    }

    private async Task<string> GetTokenId()
    {
        var tokenIdGrain = GetTokenIdGrain();
        var tokenId = await tokenIdGrain.GenerateTokenId();
        State.TokenIds.Add(tokenId);

        await WriteStateAsync();
        return tokenId;
    }

    public async Task<GrainResultDto<ItemMintInfo>> ChangeMintStatus(string itemId, FreeMintStatus status)
    {
        var mintNftInfo = State.MintInfos.FirstOrDefault(t => t.ItemId == itemId);
        if (mintNftInfo == null)
        {
            return new GrainResultDto<ItemMintInfo>
            {
                Message = "Mint Nft Item info not exist."
            };
        }

        if (status != FreeMintStatus.SUCCESS && status != FreeMintStatus.FAIL)
        {
            return new GrainResultDto<ItemMintInfo>
            {
                Message = $"Not allowed to change to {status.ToString()}."
            };
        }

        SetDateMintInfo(status, mintNftInfo.ItemId);
        mintNftInfo.Status = status;
        State.PendingTokenId = string.Empty;
        await WriteStateAsync();
        return new GrainResultDto<ItemMintInfo>(mintNftInfo);
    }

    private void SetDateMintInfo(FreeMintStatus status, string itemId)
    {
        if (status != FreeMintStatus.FAIL)
        {
            return;
        }

        var date = GetDateKey();
        var items = State.DateMintInfo.GetOrDefault(date);
        if (items.IsNullOrEmpty() || !items.Contains(itemId))
        {
            return;
        }

        items.Remove(itemId);
    }

    private ITokenIdGrain GetTokenIdGrain()
    {
        return GrainFactory.GetGrain<ITokenIdGrain>(CommonConstant.FreeMintTokenIdGrainId);
    }

    public Task<bool> CheckLimitExceed()
    {
        return Task.FromResult(IsLimitExceed());
    }

    private bool IsLimitExceed()
    {
        var date = GetDateKey();
        var mintInfos = State.DateMintInfo.GetOrDefault(date);

        if (mintInfos.IsNullOrEmpty())
        {
            return false;
        }

        return mintInfos.Count >= _freeMintOptions.LimitCount;
    }

    private string GetDateKey() => DateTime.UtcNow.ToString("yyyyMMdd");
}