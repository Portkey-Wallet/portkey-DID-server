using CAServer.Grains.State;
using Microsoft.Extensions.Options;
using Orleans;
using Orleans.Providers;
using Volo.Abp.ObjectMapping;

namespace CAServer.Grains.Grain.Account;

[StorageProvider(ProviderName = "Default")]
public class RegisterGrain : Grain<RegisterState>, IRegisterGrain
{
    private readonly IObjectMapper _objectMapper;
    private readonly CAAccountOption _cAAccountOption;

    public RegisterGrain(IObjectMapper objectMapper, IOptions<CAAccountOption> options)
    {
        _objectMapper = objectMapper;
        _cAAccountOption = options.Value;
    }

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

    public async Task<GrainResultDto<RegisterGrainDto>> RequestAsync(RegisterGrainDto recoveryDto)
    {
        var result = new GrainResultDto<RegisterGrainDto>();

        int maxLength = _cAAccountOption.CAAccountRequestInfoMaxLength;
        int expirationTime = _cAAccountOption.CAAccountRequestInfoExpirationTime;

        if (State.RegisterInfo.Count >= maxLength)
        {
            State.RegisterInfo.RemoveAll(State.RegisterInfo.Where(t =>
                t.CreateTime.HasValue && t.CreateTime.Value < DateTime.UtcNow.AddHours(-expirationTime)));
        }

        if (State.RegisterInfo.Count >= maxLength)
        {
            result.Message = RegisterMessage.OverCountMessage;
            return result;
        }

        var registerInfo = _objectMapper.Map<RegisterGrainDto, RegisterInfo>(recoveryDto);
        registerInfo.CreateTime = DateTime.UtcNow;
        State.RegisterInfo.Add(registerInfo);

        State.Id = this.GetPrimaryKeyString();
        registerInfo.GrainId = State.Id;

        await WriteStateAsync();

        result.Success = true;
        result.Data = _objectMapper.Map<RegisterInfo, RegisterGrainDto>(registerInfo);
        result.Data.Context = recoveryDto.Context;
        result.Data.ReferralInfo = recoveryDto.ReferralInfo;
        return result;
    }

    public async Task<GrainResultDto<RegisterGrainDto>> UpdateRegisterResultAsync(CreateHolderResultGrainDto resultDto)
    {
        var result = new GrainResultDto<RegisterGrainDto>();

        var register = State.RegisterInfo.FirstOrDefault(t => t.Id == resultDto.Id);
        if (register == null)
        {
            result.Message = RegisterMessage.NotFoundMessage;
            return result;
        }

        register.RegisteredTime = resultDto.RegisteredTime;
        register.RegisterMessage = resultDto.RegisterMessage;
        register.RegisterSuccess = resultDto.RegisterSuccess;
        register.CaHash = resultDto.CaHash;
        register.CaAddress = resultDto.CaAddress;

        await WriteStateAsync();
        result.Success = true;
        result.Data = _objectMapper.Map<RegisterInfo, RegisterGrainDto>(register);
        return result;
    }
}