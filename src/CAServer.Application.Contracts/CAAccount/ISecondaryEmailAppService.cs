using System;
using System.Threading.Tasks;
using CAServer.CAAccount.Cmd;
using CAServer.Transfer.Dtos;

namespace CAServer.CAAccount;

public interface ISecondaryEmailAppService
{
    public Task<VerifySecondaryEmailResponse> VerifySecondaryEmailAsync(VerifySecondaryEmailCmd cmd);

    public Task<VerifySecondaryEmailCodeResponse> VerifySecondaryEmailCodeAsync(VerifySecondaryEmailCodeCmd cmd);

    public Task<SetSecondaryEmailResponse> SetSecondaryEmailAsync(SetSecondaryEmailCmd cmd);

    public Task<GetSecondaryEmailResponse> GetSecondaryEmailAsync(Guid userId);
}