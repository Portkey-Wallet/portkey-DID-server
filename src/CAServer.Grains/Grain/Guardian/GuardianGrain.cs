using CAServer.Grains.Grain;
using CAServer.Grains.Grain.Guardian;
using CAServer.Grains.State;
using Microsoft.Extensions.Logging;
using Orleans;
using Volo.Abp.ObjectMapping;

public class GuardianGrain : Grain<GuardianState>, IGuardianGrain
{
    private readonly IObjectMapper _objectMapper;
    private readonly ILogger<GuardianGrain> _logger;

    public GuardianGrain(IObjectMapper objectMapper, ILogger<GuardianGrain> logger)
    {
        _objectMapper = objectMapper;
        _logger = logger;
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
        _logger.LogInformation("AddGuardian start, identifier:{identifier}, identifierHash :{identifierHash}", identifier, identifierHash);
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
        
        //await WriteStateAsync();
        result.Success = true;

        result.Data = _objectMapper.Map<GuardianState, GuardianGrainDto>(State);
        _logger.LogInformation("AddGuardian end, identifier:{identifier}, identifierHash :{identifierHash}", identifier, identifierHash);

        return result;
    }

    public Task<GrainResultDto<GuardianGrainDto>> GetGuardianAsync(string identifier)
    {
        _logger.LogInformation("GetGuardian start, identifier:{identifier}", identifier);
        var result = new GrainResultDto<GuardianGrainDto>();

        if (string.IsNullOrEmpty(State.IdentifierHash) || State.IsDeleted)
        {
            result.Message = "Guardian not exist.";
            return Task.FromResult(result);
        }

        result.Success = true;
        result.Data = _objectMapper.Map<GuardianState, GuardianGrainDto>(State);
        _logger.LogInformation("GetGuardian end, identifier:{identifier}", identifier);
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
        //await WriteStateAsync();
        result.Success = true;

        result.Data = _objectMapper.Map<GuardianState, GuardianGrainDto>(State);
        return result;
    }
}