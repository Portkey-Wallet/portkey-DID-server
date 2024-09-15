using System.Collections.Generic;
using System.Threading.Tasks;
using CAServer.CAActivity.Dto;
using CAServer.CAActivity.Dtos;
using CAServer.CAActivity.Provider;
using CAServer.UserAssets;
using Volo.Abp.Application.Services;

namespace CAServer.CAActivity;

public interface IUserActivityAppService:IApplicationService
{
    Task<GetActivitiesDto> GetTwoCaTransactionsAsync(GetTwoCaTransactionRequestDto request);
    Task<GetActivitiesDto> GetActivitiesAsync(GetActivitiesRequestDto request);
    Task<GetActivityDto> GetActivityAsync(GetActivityRequestDto request);
    Task<string> GetCaHolderCreateTimeAsync(GetUserCreateTimeRequestDto requestDto);

    Task<IndexerTransactions> GetTransactionByTransactionType(string transactionType);

    Task<IndexerTransactions> GetActivitiesWithBlockHeightAsync(List<string> inputTransactionTypes, string chainId, long startHeight, long endHeight);

    Task<IndexerTransactions> GetActivitiesV3(List<CAAddressInfo> caAddressInfos, string chainId);
}