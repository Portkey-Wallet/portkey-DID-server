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
            
            // var connection = new HubConnectionBuilder()
            //     .WithUrl("http://localhost:5577/dataReporting", 
            //         options => { options.AccessTokenProvider = () => Task.FromResult("eyJhbGciOiJSUzI1NiIsImtpZCI6IjI5MUREQzBBNTE5NjdGRDA0NThBQTNFNzI1NERENkVFOENGMTA3NkMiLCJ4NXQiOiJLUjNjQ2xHV2Y5QkZpcVBuSlUzVzdvenhCMnciLCJ0eXAiOiJhdCtqd3QifQ.eyJzdWIiOiIwMTdlNzZlNy04Y2UyLTQzNjItYmE4OC1iYzQxMmVmYmQyNWQiLCJvaV9wcnN0IjoiQ0FTZXJ2ZXJfQXBwIiwiY2xpZW50X2lkIjoiQ0FTZXJ2ZXJfQXBwIiwib2lfdGtuX2lkIjoiMmI5MGZlODItMDk3ZC1lNTQzLTg4MWEtM2EwZTliYzRjM2E2IiwiYXVkIjoiQ0FTZXJ2ZXIiLCJzY29wZSI6IkNBU2VydmVyIiwianRpIjoiOWQ1OTliZDUtM2ViZC00OTcwLTk5YzItOWVmOGFhMmI0N2I4IiwiZXhwIjoxNjk4OTkzMzE1LCJpc3MiOiJodHRwOi8vMTkyLjE2OC42Ni4yNDA6ODA4MC8iLCJpYXQiOjE2OTg4MjA1MTZ9.pHS9vtKgL6f9VLWLcB3uFpQuzMc0Bsn9Oc_MjgX1AOKq5wdaitSCslgqQ_-8818PoQK13O7wT2bEUMnDpDrKdQiyDvTZw7IWwjatTWI0zdouFe-dIuEf7yBkoaG68ZguJ9SaHpncBKknovFXwLB6707tnGaOWugwFBgEE9q4tSXCB8mA1E-hIsG257IFet-J1G3DxSyYIysf-tMAZ79LilpN5OjypNTphGT2GTu9yNpwQIWaauagPuX4hIKHhrg4WoRYkUJ4c6kHXUhOun5FHXIaqKabedWrZSbj7qQyHxGepSOahGi_2jOdYb_2rZg41MjV4kHiw28XWOJZLJ-VkA"); })
            //     .Build();
            
            var connection = new HubConnectionBuilder()
                .WithUrl("http://localhost:5577/communication") 
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