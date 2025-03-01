namespace CAServer.Grains.Grain.Account;

public interface IRegisterGrain : IGrainWithStringKey
{
    Task<GrainResultDto<RegisterGrainDto>> RequestAsync(RegisterGrainDto registerGrainDto);
    Task<GrainResultDto<RegisterGrainDto>> UpdateRegisterResultAsync(CreateHolderResultGrainDto result);
}