using CAServer.Commons;
using CAServer.EnumType;
using CAServer.FreeMint.Dtos;
using CAServer.Grains.State.FreeMint;
using Orleans;
using Volo.Abp.ObjectMapping;

namespace CAServer.Grains.Grain.FreeMint;

public class FreeMintGrain : Grain<FreeMintState>, IFreeMintGrain
{
    private readonly IObjectMapper _objectMapper;

    public FreeMintGrain(IObjectMapper objectMapper)
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

    public Task<GrainResultDto<FreeMintGrainDto>> GetFreeMintInfo()
    {
        var result = new GrainResultDto<FreeMintGrainDto>();
        if (string.IsNullOrEmpty(State.Id))
        {
            result.Message = "Free mint info not exist.";
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

        if (!string.IsNullOrEmpty(State.Id) && !State.MintInfos.IsNullOrEmpty())
        {
            statusDto.Status = State.MintInfos.Last().Status.ToString();
            statusDto.ItemId = State.MintInfos.Last().ItemId;
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
        if (State.Id.IsNullOrEmpty())
        {
            // State.Id = this.GetPrimaryKey().ToString();
            // State.UserId = this.GetPrimaryKey();
            // State.CollectionInfo = mintNftDto.CollectionInfo;
        }
        
        State.Id = this.GetPrimaryKey().ToString();
        State.UserId = this.GetPrimaryKey();
        State.CollectionInfo = mintNftDto.CollectionInfo;

        // send transaction in service like redpack
        // transaction info no need to save in grain

        // check token id
        var mintInfo = State.MintInfos.FirstOrDefault(t => t.TokenId == mintNftDto.ConfirmInfo.TokenId);
        // exists and not mint failed
        if (mintInfo != null && mintInfo.Status != FreeMintStatus.FAIL)
        {
            // tokenId already used
            result.Message = "Token id already used.";
            return result;
        }

        // failed, try again
        if (mintInfo != null)
        {
            mintInfo.Name = mintNftDto.ConfirmInfo.Name;
            mintInfo.ImageUrl = mintNftDto.ConfirmInfo.ImageUrl;
            mintInfo.Description = mintNftDto.ConfirmInfo.Description;
            mintInfo.Status = FreeMintStatus.PENDING;

            State.PendingTokenId = mintInfo.TokenId;
            return new GrainResultDto<ItemMintInfo>(mintInfo);
        }

        var nftInfo = new ItemMintInfo()
        {
            ItemId = Guid.NewGuid().ToString(),
            TokenId = mintNftDto.ConfirmInfo.TokenId,
            Name = mintNftDto.ConfirmInfo.Name,
            ImageUrl = mintNftDto.ConfirmInfo.ImageUrl,
            Description = mintNftDto.ConfirmInfo.Description,
            Status = FreeMintStatus.PENDING
        };

        State.PendingTokenId = mintNftDto.ConfirmInfo.TokenId;
        if (State.UnUsedTokenId == mintNftDto.ConfirmInfo.TokenId)
        {
            State.UnUsedTokenId = string.Empty;
        }
        State.MintInfos.Add(nftInfo);

        await WriteStateAsync();
        return new GrainResultDto<ItemMintInfo>(nftInfo);
    }

    public async Task<GrainResultDto<string>> GetTokenId()
    {
        if (!State.UnUsedTokenId.IsNullOrEmpty())
        {
            return new GrainResultDto<string>(State.UnUsedTokenId);
        }

        var tokenIdGrain = GetTokenIdGrain();
        var tokenId = await tokenIdGrain.GenerateTokenId();
        State.UnUsedTokenId = tokenId;

        await WriteStateAsync();
        return new GrainResultDto<string>(State.UnUsedTokenId.ToString());
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

        if (status == FreeMintStatus.NONE || status == FreeMintStatus.PENDING)
        {
            return new GrainResultDto<ItemMintInfo>
            {
                Message = $"Not allowed to change to {status.ToString()}."
            };
        }

        mintNftInfo.Status = status;
        State.PendingTokenId = string.Empty;
        return new GrainResultDto<ItemMintInfo>(mintNftInfo);
    }

    private ITokenIdGrain GetTokenIdGrain()
    {
        return GrainFactory.GetGrain<ITokenIdGrain>(CommonConstant.FreeMintTokenIdGrainId);
    }
}