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
    private const long TwoDaysMilliSeconds = 172800000;
    private readonly ICryptoGiftAppService _cryptoGiftAppService;
    private readonly INESTRepository<CryptoGiftNewUsersOnlyNumStatsIndex, string> _newUsersOnlyNumStatsRepository;
    private readonly INESTRepository<CryptoGiftOldUsersNumStatsIndex, string> _oldUsersNumStatsRepository;
    private readonly INESTRepository<CryptoGiftNewUsersOnlyDetailStatsIndex, string> _newUsersOnlyDetailRepository;
    private readonly INESTRepository<CryptoGiftOldUsersDetailStatsIndex, string> _oldUsersDetailRepository;
    private readonly ILogger<CryptoGiftStatisticsWorker> _logger;
    
    public CryptoGiftStatisticsWorker(
        ICryptoGiftAppService cryptoGiftAppService,
        INESTRepository<CryptoGiftNewUsersOnlyNumStatsIndex, string> newUsersOnlyNumStatsRepository,
        INESTRepository<CryptoGiftNewUsersOnlyDetailStatsIndex, string> newUsersOnlyDetailRepository,
        INESTRepository<CryptoGiftOldUsersNumStatsIndex, string> oldUsersNumStatsRepository,
        INESTRepository<CryptoGiftOldUsersDetailStatsIndex, string> oldUsersDetailRepository,
        ILogger<CryptoGiftStatisticsWorker> logger,
        AbpAsyncTimer timer, IServiceScopeFactory serviceScopeFactory) : base(timer, serviceScopeFactory)
    {
        _cryptoGiftAppService = cryptoGiftAppService;
        _newUsersOnlyNumStatsRepository = newUsersOnlyNumStatsRepository;
        _oldUsersNumStatsRepository = oldUsersNumStatsRepository;
        _newUsersOnlyDetailRepository = newUsersOnlyDetailRepository;
        _oldUsersDetailRepository = oldUsersDetailRepository;
        _logger = logger;
        Timer.Period = WorkerConst.CryptoGiftStatisticsPeriod;
    }

    protected override async Task DoWorkAsync(PeriodicBackgroundWorkerContext workerContext)
    {
        var current = DateTimeOffset.Now.ToUnixTimeMilliseconds();
        var startTime = current - TwoDaysMilliSeconds;
        var symbols = new string[] { "ELF", "USDT", "SGR-1" };
        var joinedSymbols = string.Join(",", symbols);
        var newUsersNumberDtos = await _cryptoGiftAppService.ComputeCryptoGiftNumber(true, symbols, startTime);
        await SaveCryptoGiftNumberStatsAsync(newUsersNumberDtos, true, joinedSymbols, current);
        
        var oldUsersNumberDtos = await _cryptoGiftAppService.ComputeCryptoGiftNumber(false, symbols, startTime);
        await SaveCryptoGiftNumberStatsAsync(oldUsersNumberDtos, false, joinedSymbols, current);

        var newUsersCryptoGiftClaimStatistics = await _cryptoGiftAppService.ComputeCryptoGiftClaimStatistics(true, symbols, 1719590400000);
        await SaveCryptoGiftDetailStatsAsync(newUsersCryptoGiftClaimStatistics, true, joinedSymbols, current);
        
        var oldUsersCryptoGiftClaimStatistics = await _cryptoGiftAppService.ComputeCryptoGiftClaimStatistics(false, symbols, 1719590400000);
        await SaveCryptoGiftDetailStatsAsync(oldUsersCryptoGiftClaimStatistics, false, joinedSymbols, current);
    }

    private async Task SaveCryptoGiftDetailStatsAsync(List<CryptoGiftClaimDto> details,
        bool newUsersOnly, string joinedSymbols, long current)
    {
        if (newUsersOnly)
        {
            foreach (var dto in details)
            {
                await _newUsersOnlyDetailRepository.AddOrUpdateAsync(new CryptoGiftNewUsersOnlyDetailStatsIndex
                {
                    Id = dto.CaAddress,
                    Symbols = joinedSymbols,
                    CaAddress = dto.CaAddress,
                    Number = dto.Number,
                    Count = dto.Count,
                    Grabbed = dto.Grabbed,
                    CreateTime = current
                });
            }
        }
        else
        {
            foreach (var dto in details)
            {
                await _oldUsersDetailRepository.AddOrUpdateAsync(new CryptoGiftOldUsersDetailStatsIndex
                {
                    Id = dto.CaAddress,
                    Symbols = joinedSymbols,
                    CaAddress = dto.CaAddress,
                    Number = dto.Number,
                    Count = dto.Count,
                    Grabbed = dto.Grabbed,
                    CreateTime = current
                });
            }
        }
    }

    private async Task SaveCryptoGiftNumberStatsAsync(List<CryptoGiftSentNumberDto> numberDtos, bool newUsersOnly, string joinedSymbols, long current)
    {
        if (newUsersOnly)
        {
            foreach (var cryptoGiftSentNumberDto in numberDtos)
            {
                try
                {
                    await _newUsersOnlyNumStatsRepository.AddOrUpdateAsync(new CryptoGiftNewUsersOnlyNumStatsIndex
                    {
                        Id = cryptoGiftSentNumberDto.Date,
                        Date = cryptoGiftSentNumberDto.Date,
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
        else
        {
            foreach (var cryptoGiftSentNumberDto in numberDtos)
            {
                try
                {
                    await _oldUsersNumStatsRepository.AddOrUpdateAsync(new CryptoGiftOldUsersNumStatsIndex
                    {
                        Id = cryptoGiftSentNumberDto.Date,
                        Date = cryptoGiftSentNumberDto.Date,
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
}