using System;
using System.Threading.Tasks;
using CAServer.CAAccount.Cmd;
using CAServer.Transfer.Dtos;

namespace CAServer.CAAccount;

public interface ISecondaryEmailAppService
{
    public Task<ResponseWrapDto<VerifySecondaryEmailResponse>> VerifySecondaryEmailAsync(VerifySecondaryEmailCmd cmd);

    public Task<ResponseWrapDto<VerifySecondaryEmailCodeResponse>> VerifySecondaryEmailCodeAsync(VerifySecondaryEmailCodeCmd cmd);

    public Task<ResponseWrapDto<SetSecondaryEmailResponse>> SetSecondaryEmailAsync(SetSecondaryEmailCmd cmd);

    public Task<ResponseWrapDto<GetSecondaryEmailResponse>> GetSecondaryEmailAsync(Guid userId);
}