using System;
using System.Threading.Tasks;
using CAServer.CAAccount.Dtos;
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
                .WithUrl("http://localhost:5001/dataReporting", 
                    options => { options.AccessTokenProvider = () => Task.FromResult("eyJhbGciOiJSUzI1NiIsImtpZCI6IjI5MUREQzBBNTE5NjdGRDA0NThBQTNFNzI1NERENkVFOENGMTA3NkMiLCJ4NXQiOiJLUjNjQ2xHV2Y5QkZpcVBuSlUzVzdvenhCMnciLCJ0eXAiOiJhdCtqd3QifQ.eyJzdWIiOiJlNDViOWIwZi0xZGI2LTQ1NDEtYWRmYy1lNjNhMzhkYzNlZGMiLCJvaV9wcnN0IjoiQ0FTZXJ2ZXJfQXBwIiwiY2xpZW50X2lkIjoiQ0FTZXJ2ZXJfQXBwIiwib2lfdGtuX2lkIjoiYTUzMGQ5MWMtMzdhZC0yYTRlLWExMWEtM2EwZTkxYWJiNmIwIiwiYXVkIjoiQ0FTZXJ2ZXIiLCJzY29wZSI6IkNBU2VydmVyIiwianRpIjoiMjJjYjhhNzAtMmM4NS00MDQ2LTk4OTYtNDlhNjBiMmU4NzY1IiwiZXhwIjoxNjk4ODIzOTAxLCJpc3MiOiJodHRwOi8vMTkyLjE2OC42Ni4yNDA6ODA4MC8iLCJpYXQiOjE2OTg2NTExMDJ9.gQyXrSaLwcisV8qWM4e1fo60Rh5JZXgyQ4wBA4IpQDoDmi-ZWPlFGk7uSq3xebMXq6HpH3_EM268HYJCH4Q9O7ZBKuPEucZiSsxIRnBmKmtQqPov8O2-mOzLDPWujuxekvpGp7K7M-mIJ4G3Z815PDnaLK5hQYWoSgG4dAKXLxYInKjfpqBeGi3-G8PhKhzpuKB9yAGcPrbe-G6SVhfUt-P7omLQLUNCdsgMNHJt-itMSSeD_H-qstn9X8N4oG2wpkKbvKwxm62PqUYT42yYnSL36EuqZp2_-Sj6LCHliE9AwFSgsKtYdZhtoeqYtr26-TFpqe7xSXuXO6hwj22vMw"); })
                .Build();

            //connection.On<HubResponse<string>>("Sin2", s => { Console.WriteLine($"Receive ping, requestId={s.RequestId} body={s.Body}"); });
            // connection.On<HubResponse<CAServer.Dtos.RegisterCompletedMessageDto>>("caAccountRegister",          
            //     s =>
            //     {
            //         Console.WriteLine(
            //             $"Receive ping, requestId={s.RequestId} body={JsonConvert.SerializeObject(s.Body)}");
            //     });

            connection.On<HubResponse<RecoveryCompletedMessageDto>>("caAccountRecover",
                s =>
                {
                    Console.WriteLine(
                        $"Receive ping, requestId={s.RequestId} body={JsonConvert.SerializeObject(s.Body)}");
                });

            await connection.StartAsync().ConfigureAwait(false);

            await connection.InvokeAsync("Connect",
                "client_646411");
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }
}