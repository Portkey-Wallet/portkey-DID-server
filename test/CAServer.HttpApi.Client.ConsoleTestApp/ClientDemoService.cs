using System;
using System.Threading.Tasks;
using CAServer.Hubs;
using Volo.Abp.Account;
using Volo.Abp.DependencyInjection;
using Microsoft.AspNetCore.SignalR.Client;
using Newtonsoft.Json;

namespace CAServer.HttpApi.Client.ConsoleTestApp;

public class ClientDemoService : ITransientDependency
{
    private readonly IProfileAppService _profileAppService;

    public ClientDemoService(IProfileAppService profileAppService)
    {
        _profileAppService = profileAppService;
    }

    public async Task RunAsync()
    {
        var output = await _profileAppService.GetAsync();
        Console.WriteLine($"UserName : {output.UserName}");
        Console.WriteLine($"Email    : {output.Email}");
        Console.WriteLine($"Name     : {output.Name}");
        Console.WriteLine($"Surname  : {output.Surname}");
    }

    public async Task RunHubClientAsync()
    {
        try
        {
            var connection = new HubConnectionBuilder()
                .WithUrl("http://127.0.0.1:5577/ca")
                .Build();

            //connection.On<HubResponse<string>>("Sin2", s => { Console.WriteLine($"Receive ping, requestId={s.RequestId} body={s.Body}"); });
            connection.On<HubResponse<string>>("Ping",          
                s =>
                {
                    Console.WriteLine(
                        $"Receive ping, requestId={s.RequestId} body={JsonConvert.SerializeObject(s.Body)}");
                });

            await connection.StartAsync().ConfigureAwait(false);

            await connection.InvokeAsync("Connect",
                "client_64641");
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }
}