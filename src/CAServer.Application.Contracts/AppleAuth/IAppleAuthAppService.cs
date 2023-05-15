using System.Threading.Tasks;
using CAServer.AppleAuth.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace CAServer.AppleAuth;

public interface IAppleAuthAppService
{
    Task ReceiveAsync(AppleAuthDto appleAuthDto);
    Task<RedirectResult> ReceiveTestAsync(AppleAuthDto appleAuthDto);
}