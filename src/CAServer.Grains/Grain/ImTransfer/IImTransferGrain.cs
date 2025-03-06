namespace CAServer.Grains.Grain.ImTransfer;

public interface IImTransferGrain : IGrainWithStringKey
{
    Task<GrainResultDto<TransferGrainDto>> CreateTransfer(TransferGrainDto transferGrainDto);
    Task<GrainResultDto<TransferGrainDto>> UpdateTransfer(TransferGrainDto transferGrainDto);
    Task<GrainResultDto<TransferGrainDto>> GetTransfer();
}