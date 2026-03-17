using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CAServer.Cache;
using CAServer.Options;
using CAServer.Verifier;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using StackExchange.Redis;
using Xunit;

namespace CAServer.VerifierCode;

public class RegistrationEmailRateLimitServiceTests
{
    [Fact]
    public void ShouldApply_ReturnsExpectedResult()
    {
        var service = CreateService();

        Assert.True(service.ShouldApply("Email", OperationType.CreateCAHolder));
        Assert.True(service.ShouldApply("email", OperationType.SocialRecovery));
        Assert.False(service.ShouldApply("Phone", OperationType.CreateCAHolder));
        Assert.False(service.ShouldApply("Email", OperationType.Approve));
    }

    [Fact]
    public void ShouldApply_ReturnsFalse_When_Disabled()
    {
        var service = CreateService(new RegistrationEmailRateLimitOptions
        {
            IsEnabled = false
        });

        Assert.False(service.ShouldApply("Email", OperationType.CreateCAHolder));
    }

    [Fact]
    public async Task CheckAsync_Blocks_CreateCAHolder_After_Default_Ten_Minute_Limit()
    {
        var service = CreateService();

        RegistrationEmailRateLimitCheckResult result = null;
        for (var i = 0; i < 11; i++)
        {
            result = await service.CheckAsync("1.1.1.1", OperationType.CreateCAHolder, "trace-create");
        }

        Assert.NotNull(result);
        Assert.False(result.IsAllowed);
        Assert.Equal("CreateCAHolder:10m", result.WindowName);
        Assert.Equal(10, result.Limit);
        Assert.Equal(11, result.Count);
        Assert.True(result.RetryAfterSeconds > 0);
    }

    [Fact]
    public async Task CheckAsync_Blocks_SocialRecovery_After_Default_Ten_Minute_Limit()
    {
        var service = CreateService();

        RegistrationEmailRateLimitCheckResult result = null;
        for (var i = 0; i < 16; i++)
        {
            result = await service.CheckAsync("2.2.2.2", OperationType.SocialRecovery, "trace-recovery");
        }

        Assert.NotNull(result);
        Assert.False(result.IsAllowed);
        Assert.Equal("SocialRecovery:10m", result.WindowName);
        Assert.Equal(15, result.Limit);
        Assert.Equal(16, result.Count);
        Assert.True(result.RetryAfterSeconds > 0);
    }

    [Fact]
    public async Task CheckAsync_Blocks_When_Hour_Window_Is_Exceeded()
    {
        var options = new RegistrationEmailRateLimitOptions
        {
            IsEnabled = true,
            CreateCAHolder = new RegistrationEmailRateLimitRuleOptions
            {
                Per10Minutes = 100,
                PerHour = 3
            }
        };
        var service = CreateService(options);

        RegistrationEmailRateLimitCheckResult result = null;
        for (var i = 0; i < 4; i++)
        {
            result = await service.CheckAsync("3.3.3.3", OperationType.CreateCAHolder, "trace-hour");
        }

        Assert.NotNull(result);
        Assert.False(result.IsAllowed);
        Assert.Equal("CreateCAHolder:1h", result.WindowName);
        Assert.Equal(3, result.Limit);
        Assert.Equal(4, result.Count);
        Assert.True(result.RetryAfterSeconds > 0);
    }

    [Fact]
    public async Task CheckAsync_Fails_Open_When_Cache_Throws()
    {
        var service = CreateService(cacheProvider: new ThrowingCacheProvider());

        var result = await service.CheckAsync("4.4.4.4", OperationType.CreateCAHolder, "trace-fail-open");

        Assert.True(result.IsAllowed);
    }

    [Fact]
    public async Task CheckAsync_ReturnsAllow_When_Disabled()
    {
        var service = CreateService(new RegistrationEmailRateLimitOptions
        {
            IsEnabled = false
        }, new ThrowingCacheProvider());

        var result = await service.CheckAsync("5.5.5.5", OperationType.CreateCAHolder, "trace-disabled");

        Assert.True(result.IsAllowed);
    }

    private static RegistrationEmailRateLimitService CreateService(
        RegistrationEmailRateLimitOptions options = null,
        ICacheProvider cacheProvider = null)
    {
        var optionsSnapshot = new Mock<IOptionsSnapshot<RegistrationEmailRateLimitOptions>>();
        optionsSnapshot.Setup(x => x.Value).Returns(options ?? new RegistrationEmailRateLimitOptions
        {
            IsEnabled = true
        });

        return new RegistrationEmailRateLimitService(
            cacheProvider ?? new InMemoryCounterCacheProvider(),
            Mock.Of<ILogger<RegistrationEmailRateLimitService>>(),
            optionsSnapshot.Object);
    }

    private class InMemoryCounterCacheProvider : ICacheProvider
    {
        private readonly Dictionary<string, long> _counters = new();

        public virtual Task<long> Increase(string key, int increase, TimeSpan? expire)
        {
            _counters.TryGetValue(key, out var count);
            count += increase;
            _counters[key] = count;
            return Task.FromResult(count);
        }

        public Task HSetWithExpire(string key, string member, string value, TimeSpan? expire)
        {
            throw new NotImplementedException();
        }

        public Task<bool> HashDeleteAsync(string key, string member)
        {
            throw new NotImplementedException();
        }

        public Task<HashEntry[]> HGetAll(string key)
        {
            throw new NotImplementedException();
        }

        public Task Set(string key, string value, TimeSpan? expire)
        {
            throw new NotImplementedException();
        }

        public Task Set<T>(string key, T value, TimeSpan? expire) where T : class
        {
            throw new NotImplementedException();
        }

        public Task<RedisValue> Get(string key)
        {
            throw new NotImplementedException();
        }

        public Task<T> Get<T>(string key) where T : class
        {
            throw new NotImplementedException();
        }

        public Task Delete(string key)
        {
            throw new NotImplementedException();
        }

        public Task<Dictionary<string, RedisValue>> BatchGet(List<string> keys)
        {
            throw new NotImplementedException();
        }

        public Task AddScoreAsync(string leaderboardKey, string member, double score)
        {
            throw new NotImplementedException();
        }

        public Task<double> GetScoreAsync(string leaderboardKey, string member)
        {
            throw new NotImplementedException();
        }

        public Task<long> GetRankAsync(string leaderboardKey, string member, bool highToLow = true)
        {
            throw new NotImplementedException();
        }

        public Task<SortedSetEntry[]> GetTopAsync(string leaderboardKey, long startRank, long stopRank,
            bool highToLow = true)
        {
            throw new NotImplementedException();
        }

        public Task<long> GetSortedSetLengthAsync(string leaderboardKey)
        {
            throw new NotImplementedException();
        }

        public Task SetAddAsync(string key, string value, TimeSpan? timeSpan)
        {
            throw new NotImplementedException();
        }

        public Task SetAddAsync(string key, List<string> values, TimeSpan? timeSpan)
        {
            throw new NotImplementedException();
        }

        public Task SetRemoveAsync(string key, List<string> values)
        {
            throw new NotImplementedException();
        }

        public Task<RedisValue[]> SetMembersAsync(string key)
        {
            throw new NotImplementedException();
        }
    }

    private sealed class ThrowingCacheProvider : InMemoryCounterCacheProvider
    {
        public override Task<long> Increase(string key, int increase, TimeSpan? expire)
        {
            throw new InvalidOperationException("redis unavailable");
        }
    }
}
