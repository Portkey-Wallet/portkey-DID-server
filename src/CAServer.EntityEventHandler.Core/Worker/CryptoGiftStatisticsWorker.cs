using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using CAServer.CryptoGift;
using CAServer.CryptoGift.Dtos;
using CAServer.Entities.Es;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.Threading;

namespace CAServer.EntityEventHandler.Core.Worker;

public class CryptoGiftStatisticsWorker : AsyncPeriodicBackgroundWorkerBase
{
    private readonly ICryptoGiftAppService _cryptoGiftAppService;
    private readonly INESTRepository<CryptoGiftNumStatsIndex, string> _cryptoGiftNumIndexRepository;
    private readonly INESTRepository<CryptoGiftDetailStatsIndex, string> _cryptoGiftDetailRepository;
    private readonly ILogger<CryptoGiftStatisticsWorker> _logger;
    
    public CryptoGiftStatisticsWorker(
        ICryptoGiftAppService cryptoGiftAppService,
        INESTRepository<CryptoGiftNumStatsIndex, string> cryptoGiftNumIndexRepository,
        INESTRepository<CryptoGiftDetailStatsIndex, string> cryptoGiftDetailRepository,
        ILogger<CryptoGiftStatisticsWorker> logger,
        AbpAsyncTimer timer, IServiceScopeFactory serviceScopeFactory) : base(timer, serviceScopeFactory)
    {
        _cryptoGiftAppService = cryptoGiftAppService;
        _cryptoGiftNumIndexRepository = cryptoGiftNumIndexRepository;
        _cryptoGiftDetailRepository = cryptoGiftDetailRepository;
        _logger = logger;
        Timer.Period = WorkerConst.CryptoGiftStatisticsPeriod;
    }

    protected override async Task DoWorkAsync(PeriodicBackgroundWorkerContext workerContext)
    {
        var current = DateTimeOffset.Now.ToUnixTimeMilliseconds();
        var symbols = new string[] { "ELF", "USDT", "SGR-1" };
        var joinedSymbols = string.Join(",", symbols);
        var newUsersNumberDtos = await _cryptoGiftAppService.ComputeCryptoGiftNumber(true, symbols, 1719590400000);
        await SaveCryptoGiftNumberStatsAsync(newUsersNumberDtos, true, joinedSymbols, current);
        
        var oldUsersNumberDtos = await _cryptoGiftAppService.ComputeCryptoGiftNumber(false, symbols, 1719590400000);
        await SaveCryptoGiftNumberStatsAsync(oldUsersNumberDtos, false, joinedSymbols, current);

        var newUsersCryptoGiftClaimStatistics = await _cryptoGiftAppService.ComputeCryptoGiftClaimStatistics(true, symbols, 1719590400000);
        await SaveCryptoGiftDetailStatsAsync(newUsersCryptoGiftClaimStatistics, true, joinedSymbols, current);
        
        var oldUsersCryptoGiftClaimStatistics = await _cryptoGiftAppService.ComputeCryptoGiftClaimStatistics(false, symbols, 1719590400000);
        await SaveCryptoGiftDetailStatsAsync(oldUsersCryptoGiftClaimStatistics, false, joinedSymbols, current);

        await Task.Delay(TimeSpan.FromSeconds(10));
        var nums = await _cryptoGiftNumIndexRepository.GetListAsync();
        _logger.LogInformation("SaveCryptoGiftNumberStatsAsync:{0}", JsonConvert.SerializeObject(nums));
        var details = await _cryptoGiftDetailRepository.GetListAsync();
        _logger.LogInformation("SaveCryptoGiftDetailStatsAsync:{0}", JsonConvert.SerializeObject(details));
    }

    private async Task SaveCryptoGiftDetailStatsAsync(List<CryptoGiftClaimDto> details,
        bool newUsersOnly, string joinedSymbols, long current)
    {
        foreach (var dto in details)
        {
            await _cryptoGiftDetailRepository.AddOrUpdateAsync(new CryptoGiftDetailStatsIndex
            {
                Id = string.Join(":", new List<string>(){dto.CaAddress, newUsersOnly.ToString()}),
                IsNewUsersOnly = newUsersOnly,
                Symbols = joinedSymbols,
                CaAddress = dto.CaAddress,
                Number = dto.Number,
                Count = dto.Count,
                Grabbed = dto.Grabbed,
                CreateTime = current
            });
        }
    }

    private async Task SaveCryptoGiftNumberStatsAsync(List<CryptoGiftSentNumberDto> numberDtos, bool newUsersOnly, string joinedSymbols, long current)
    {
        foreach (var cryptoGiftSentNumberDto in numberDtos)
        {
            try
            {
                await _cryptoGiftNumIndexRepository.AddOrUpdateAsync(new CryptoGiftNumStatsIndex
                {
                    Id = string.Join(":", new List<string>(){cryptoGiftSentNumberDto.Date, newUsersOnly.ToString()}),
                    Date = cryptoGiftSentNumberDto.Date,
                    IsNewUsersOnly = newUsersOnly,
                    Number = cryptoGiftSentNumberDto.Number,
                    Symbols = joinedSymbols,
                    CreateTime = current
                });
            }
            catch (Exception e)
            {
                _logger.LogError(e, "add or update crypto gift error, cryptoGiftSentNumberDto:{0}", JsonConvert.SerializeObject(cryptoGiftSentNumberDto));
            }
        }
    }
}