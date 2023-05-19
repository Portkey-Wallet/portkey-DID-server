using System.Collections.Generic;
using System.Threading.Tasks;
using CAServer.CAActivity.Dto;
using CAServer.CAActivity.Dtos;
using Volo.Abp.Application.Services;

namespace CAServer.CAActivity;

public interface IUserActivityAppService:IApplicationService
{
    Task<GetActivitiesDto> GetTwoCaTransactionsAsync(GetActivitiesRequestDto request);
    Task<GetActivitiesDto> GetActivitiesAsync(GetActivitiesRequestDto request);
    Task<GetActivityDto> GetActivityAsync(GetActivityRequestDto request);
}