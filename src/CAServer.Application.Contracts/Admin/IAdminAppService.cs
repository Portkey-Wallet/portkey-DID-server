using System;
using System.Threading.Tasks;
using CAServer.Admin.Dtos;
using Volo.Abp.DependencyInjection;

namespace CAServer.Admin;

public interface IAdminAppService: ITransientDependency
{


    public Task<AdminUserResponse> GetCurrentUserAsync();
    
    public MfaResponse GenerateRandomMfa();

    public Task SetMfaAsync(MfaRequest mfaRequest);

    public Task ClearMfaAsync(Guid userId);
    
    public Task AssertMfaAsync(string code);


}