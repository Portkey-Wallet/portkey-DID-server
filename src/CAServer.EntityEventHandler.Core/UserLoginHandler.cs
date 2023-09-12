using System;
using System.Threading.Tasks;
using CAServer.Etos;
using Volo.Abp.EventBus.Distributed;

namespace CAServer.EntityEventHandler.Core;

public class UserLoginHandler : IDistributedEventHandler<UserLoginEto>
{
    public async Task HandleEventAsync(UserLoginEto eventData)
    {
        throw new NotImplementedException();
    }
}