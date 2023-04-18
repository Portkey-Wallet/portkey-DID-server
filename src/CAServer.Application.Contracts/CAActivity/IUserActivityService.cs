using System.Collections.Generic;
using System.Threading.Tasks;
using CAServer.CAActivity.Dto;
using CAServer.CAActivity.Dtos;
using Volo.Abp.Application.Services;

namespace CAServer.CAActivity;

public interface IUserActivityAppService:IApplicationService
{
    Task<List<GetActivitiesDto>> GetActivitiesAsync(GetActivitiesRequestDto request);
    Task<GetActivitiesDto> GetActivityAsync(GetActivityRequestDto request);
}