using System;
using System.Threading.Tasks;
using CAServer.Admin.Dtos;
using Volo.Abp.DependencyInjection;

namespace CAServer.Admin;

public interface IAdminAppService: ITransientDependency
{


    public Task<AdminUserResponse> GetCurrentUserAsync();
    
    public MfaResponse GenerateRandomMfa();

    public Task SetMfa(MfaRequest mfaRequest);

    public Task ClearMfa(Guid userId);
    
    public Task AssertMfa(string code);


}