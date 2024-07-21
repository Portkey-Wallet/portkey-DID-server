using System.Threading.Tasks;
using Volo.Abp;
using AElf.CSharp.Core;
using AElf.Types;
using AetherLink.Contracts.Oracle;
using Portkey.Contracts.CA;

namespace CAServer.ZkLogin;

[RemoteService(false)]
public class ZkLoginAdminService : CAServerAppService, IZkLoginAdminService
{
    // public async Task Test()
    // {
    //     var result = await OracleContractStub.CreateSubscriptionWithConsumer.SendAsync(ConsumerContractAddress);
    // }
    //
    // public async Task Test1()
    // {
    //     StartOracleRequest(AetherLink.Contracts.Consumer.StartOracleRequestInput input)
    // }
}