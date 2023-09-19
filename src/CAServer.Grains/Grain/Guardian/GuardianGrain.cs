using CAServer.Grains.Grain;
using CAServer.Grains.Grain.Guardian;
using CAServer.Grains.State;
using Orleans;
using Volo.Abp.ObjectMapping;

public class GuardianGrain : Grain<GuardianState>, IGuardianGrain
{
    private readonly IObjectMapper _objectMapper;

    public GuardianGrain(IObjectMapper objectMapper)
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

    public async Task<GrainResultDto<GuardianGrainDto>> AddGuardianAsync(string identifier, string salt,
        string identifierHash, string originalIdentifier = "")
    {
        var result = new GrainResultDto<GuardianGrainDto>();

        if (!string.IsNullOrEmpty(State.IdentifierHash) && !State.IsDeleted)
        {
            result.Message = "Guardian hash info has already exist.";
            result.Data = _objectMapper.Map<GuardianState, GuardianGrainDto>(State);
            return result;
        }

        State.Id = this.GetPrimaryKeyString();
        State.Identifier = identifier;
        State.OriginalIdentifier = originalIdentifier;
        State.Salt = salt;
        State.IdentifierHash = identifierHash;
        State.IsDeleted = false;
        
        await WriteStateAsync();
        result.Success = true;

        result.Data = _objectMapper.Map<GuardianState, GuardianGrainDto>(State);
        return result;
    }

    public Task<GrainResultDto<GuardianGrainDto>> GetGuardianAsync(string identifier)
    {
        var result = new GrainResultDto<GuardianGrainDto>();

        if (string.IsNullOrEmpty(State.IdentifierHash) || State.IsDeleted)
        {
            result.Message = "Guardian not exist.";
            return Task.FromResult(result);
        }

        result.Success = true;
        result.Data = _objectMapper.Map<GuardianState, GuardianGrainDto>(State);
        return Task.FromResult(result);
    }

    public async Task<GrainResultDto<GuardianGrainDto>> DeleteGuardian()
    {
        var result = new GrainResultDto<GuardianGrainDto>();

        if (string.IsNullOrEmpty(State.IdentifierHash) || State.IsDeleted)
        {
            result.Message = "Guardian not exist.";
            return result;
        }

        State.IsDeleted = true;
        await WriteStateAsync();
        result.Success = true;

        result.Data = _objectMapper.Map<GuardianState, GuardianGrainDto>(State);
        return result;
    }
}