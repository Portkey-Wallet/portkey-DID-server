using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CAServer.Cache;
using CAServer.CAAccount.Dtos;
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
    public void GetPolicy_ReturnsExpectedResult()
    {
        var service = CreateService();

        Assert.NotNull(service.GetPolicy(CreateContext("Email", OperationType.CreateCAHolder)));
        Assert.NotNull(service.GetPolicy(CreateContext("email", OperationType.SocialRecovery)));
        Assert.Null(service.GetPolicy(CreateContext("Phone", OperationType.CreateCAHolder)));
        Assert.Null(service.GetPolicy(CreateContext("Email", OperationType.Approve)));
    }

    [Fact]
    public void GetPolicy_ReturnsNull_When_Disabled()
    {
        var service = CreateService(new RegistrationEmailRateLimitOptions
        {
            IsEnabled = false
        });

        Assert.Null(service.GetPolicy(CreateContext("Email", OperationType.CreateCAHolder)));
    }

    [Fact]
    public void GetPolicy_ReturnsNull_When_Rule_Has_No_Effective_Window()
    {
        var service = CreateService(new RegistrationEmailRateLimitOptions
        {
            IsEnabled = true,
            Policies = new Dictionary<OperationType, RegistrationEmailRateLimitPolicyOptions>
            {
                [OperationType.CreateCAHolder] = new()
                {
                    GuardianType = GuardianIdentifierType.Email.ToString(),
                    Per10Minutes = 0,
                    PerHour = 0
                }
            }
        });

        Assert.Null(service.GetPolicy(CreateContext("Email", OperationType.CreateCAHolder)));
    }

    [Fact]
    public async Task CheckAsync_Blocks_CreateCAHolder_After_Default_Ten_Minute_Limit()
    {
        var service = CreateService();

        RegistrationEmailRateLimitCheckResult result = null;
        for (var i = 0; i < 11; i++)
        {
            result = await service.CheckAsync(CreateContext("Email", OperationType.CreateCAHolder, "1.1.1.1",
                "trace-create"));
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
            result = await service.CheckAsync(CreateContext("Email", OperationType.SocialRecovery, "2.2.2.2",
                "trace-recovery"));
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
        var options = CreateOptions();
        options.Policies[OperationType.CreateCAHolder] = new RegistrationEmailRateLimitPolicyOptions
        {
            GuardianType = GuardianIdentifierType.Email.ToString(),
            Per10Minutes = 100,
            PerHour = 3
        };
        var service = CreateService(options);

        RegistrationEmailRateLimitCheckResult result = null;
        for (var i = 0; i < 4; i++)
        {
            result = await service.CheckAsync(CreateContext("Email", OperationType.CreateCAHolder, "3.3.3.3",
                "trace-hour"));
        }

        Assert.NotNull(result);
        Assert.False(result.IsAllowed);
        Assert.Equal("CreateCAHolder:1h", result.WindowName);
        Assert.Equal(3, result.Limit);
        Assert.Equal(4, result.Count);
        Assert.True(result.RetryAfterSeconds > 0);
    }

    [Fact]
    public async Task CheckAsync_Returns_Longest_RetryAfter_When_Multiple_Windows_Are_Exceeded()
    {
        var fixedNow = new DateTime(2026, 1, 1, 10, 5, 0, DateTimeKind.Utc);
        var options = CreateOptions();
        options.Policies[OperationType.CreateCAHolder] = new RegistrationEmailRateLimitPolicyOptions
        {
            GuardianType = GuardianIdentifierType.Email.ToString(),
            Per10Minutes = 1,
            PerHour = 1
        };
        var service = CreateServiceWithUtcNowSequence(
            new[] { fixedNow, fixedNow },
            options,
            new InMemoryCounterCacheProvider());

        await service.CheckAsync(CreateContext("Email", OperationType.CreateCAHolder, "3.3.3.3", "trace-first"));
        var result = await service.CheckAsync(CreateContext("Email", OperationType.CreateCAHolder, "3.3.3.3",
            "trace-second"));

        Assert.NotNull(result);
        Assert.False(result.IsAllowed);
        Assert.Equal("CreateCAHolder:1h", result.WindowName);
        Assert.Equal(1, result.Limit);
        Assert.Equal(2, result.Count);
        Assert.Equal(3300, result.RetryAfterSeconds);
    }

    [Fact]
    public async Task CheckAsync_Uses_Same_Timestamp_For_All_Windows_In_One_Request()
    {
        var cacheProvider = new RecordingCacheProvider();
        var options = CreateOptions();
        options.Policies[OperationType.CreateCAHolder] = new RegistrationEmailRateLimitPolicyOptions
        {
            GuardianType = GuardianIdentifierType.Email.ToString(),
            Per10Minutes = 10,
            PerHour = 10
        };
        var service = CreateServiceWithUtcNowSequence(
            new[]
            {
                new DateTime(2026, 1, 1, 10, 59, 59, 999, DateTimeKind.Utc),
                new DateTime(2026, 1, 1, 11, 0, 0, 1, DateTimeKind.Utc)
            },
            options,
            cacheProvider);

        var result = await service.CheckAsync(CreateContext("Email", OperationType.CreateCAHolder, "7.7.7.7",
            "trace-rollover"));

        Assert.True(result.IsAllowed);
        Assert.Equal(1, service.UtcNowCallCount);
        Assert.Equal(2, cacheProvider.Keys.Count);
        Assert.Contains("RegistrationEmailRateLimit:CreateCAHolder:CreateCAHolder:10m:202601011050:7.7.7.7",
            cacheProvider.Keys);
        Assert.Contains("RegistrationEmailRateLimit:CreateCAHolder:CreateCAHolder:1h:202601011000:7.7.7.7",
            cacheProvider.Keys);
    }

    [Fact]
    public async Task CheckAsync_Fails_Open_When_Cache_Throws()
    {
        var service = CreateService(cacheProvider: new ThrowingCacheProvider());

        var result = await service.CheckAsync(CreateContext("Email", OperationType.CreateCAHolder, "4.4.4.4",
            "trace-fail-open"));

        Assert.True(result.IsAllowed);
    }

    [Fact]
    public async Task CheckAsync_Returns_Block_When_Later_Window_Throws_After_Earlier_Window_Is_Exceeded()
    {
        var fixedNow = new DateTime(2026, 1, 1, 10, 5, 0, DateTimeKind.Utc);
        var options = CreateOptions();
        options.Policies[OperationType.CreateCAHolder] = new RegistrationEmailRateLimitPolicyOptions
        {
            GuardianType = GuardianIdentifierType.Email.ToString(),
            Per10Minutes = 1,
            PerHour = 1
        };
        var service = CreateServiceWithUtcNowSequence(
            new[] { fixedNow, fixedNow },
            options,
            new ThrowOnMatchedWindowCacheProvider(":1h:", 2));

        await service.CheckAsync(CreateContext("Email", OperationType.CreateCAHolder, "4.4.4.4", "trace-first"));
        var result = await service.CheckAsync(CreateContext("Email", OperationType.CreateCAHolder, "4.4.4.4",
            "trace-partial-failure"));

        Assert.NotNull(result);
        Assert.False(result.IsAllowed);
        Assert.Equal("CreateCAHolder:10m", result.WindowName);
        Assert.Equal(1, result.Limit);
        Assert.Equal(2, result.Count);
        Assert.Equal(300, result.RetryAfterSeconds);
    }

    [Fact]
    public async Task CheckAsync_Fails_Open_When_Later_Window_Throws_Before_Any_Window_Is_Exceeded()
    {
        var fixedNow = new DateTime(2026, 1, 1, 10, 5, 0, DateTimeKind.Utc);
        var options = CreateOptions();
        options.Policies[OperationType.CreateCAHolder] = new RegistrationEmailRateLimitPolicyOptions
        {
            GuardianType = GuardianIdentifierType.Email.ToString(),
            Per10Minutes = 10,
            PerHour = 10
        };
        var service = CreateServiceWithUtcNowSequence(new[] { fixedNow }, options,
            new ThrowOnMatchedWindowCacheProvider(":1h:", 1));

        var result = await service.CheckAsync(CreateContext("Email", OperationType.CreateCAHolder, "5.5.5.5",
            "trace-fail-open"));

        Assert.True(result.IsAllowed);
    }

    [Fact]
    public async Task CheckAsync_ReturnsAllow_When_Disabled()
    {
        var service = CreateService(new RegistrationEmailRateLimitOptions
        {
            IsEnabled = false
        }, new ThrowingCacheProvider());

        var result = await service.CheckAsync(CreateContext("Email", OperationType.CreateCAHolder, "5.5.5.5",
            "trace-disabled"));

        Assert.True(result.IsAllowed);
    }

    [Fact]
    public async Task CheckAsync_ReturnsAllow_When_Rule_Has_No_Effective_Window()
    {
        var cacheProvider = new Mock<ICacheProvider>();
        var service = CreateService(new RegistrationEmailRateLimitOptions
        {
            IsEnabled = true,
            Policies = new Dictionary<OperationType, RegistrationEmailRateLimitPolicyOptions>
            {
                [OperationType.CreateCAHolder] = new()
                {
                    GuardianType = GuardianIdentifierType.Email.ToString(),
                    Per10Minutes = 0,
                    PerHour = 0
                }
            }
        }, cacheProvider.Object);

        var result = await service.CheckAsync(CreateContext("Email", OperationType.CreateCAHolder, "6.6.6.6",
            "trace-no-window"));

        Assert.True(result.IsAllowed);
        cacheProvider.Verify(x => x.Increase(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<TimeSpan?>()),
            Times.Never);
    }

    [Fact]
    public async Task CheckAsync_Should_Throw_When_ClientIp_Is_Blank_And_Policy_Applies()
    {
        var service = CreateService();

        await Assert.ThrowsAsync<ArgumentException>(() => service.CheckAsync(CreateContext("Email",
            OperationType.CreateCAHolder, string.Empty, "trace-blank-ip")));
    }

    [Fact]
    public void OptionsValidator_Should_Fail_When_Enabled_And_Policies_Are_Missing()
    {
        var validator = new RegistrationEmailRateLimitOptionsValidator();

        var result = validator.Validate(string.Empty, new RegistrationEmailRateLimitOptions
        {
            IsEnabled = true,
            Policies = new Dictionary<OperationType, RegistrationEmailRateLimitPolicyOptions>()
        });

        Assert.True(result.Failed);
    }

    [Fact]
    public void OptionsValidator_Should_Fail_When_Policy_Has_Invalid_Settings()
    {
        var validator = new RegistrationEmailRateLimitOptionsValidator();

        var result = validator.Validate(string.Empty, new RegistrationEmailRateLimitOptions
        {
            IsEnabled = true,
            Policies = new Dictionary<OperationType, RegistrationEmailRateLimitPolicyOptions>
            {
                [OperationType.CreateCAHolder] = new()
                {
                    GuardianType = string.Empty,
                    Per10Minutes = -1,
                    PerHour = -1
                }
            }
        });

        Assert.True(result.Failed);
    }

    private static RegistrationEmailRateLimitOptions CreateOptions()
    {
        return new RegistrationEmailRateLimitOptions
        {
            IsEnabled = true,
            Policies = new Dictionary<OperationType, RegistrationEmailRateLimitPolicyOptions>
            {
                [OperationType.CreateCAHolder] = new()
                {
                    GuardianType = GuardianIdentifierType.Email.ToString(),
                    Per10Minutes = 10,
                    PerHour = 30
                },
                [OperationType.SocialRecovery] = new()
                {
                    GuardianType = GuardianIdentifierType.Email.ToString(),
                    Per10Minutes = 15,
                    PerHour = 45,
                    RequireGuardianExistsBeforeConsume = true
                }
            }
        };
    }

    private static RegistrationEmailRateLimitContext CreateContext(string guardianType, OperationType operationType,
        string clientIp = "1.1.1.1", string traceId = "trace-id")
    {
        return new RegistrationEmailRateLimitContext
        {
            GuardianType = guardianType,
            OperationType = operationType,
            ClientIp = clientIp,
            TraceId = traceId
        };
    }

    private static RegistrationEmailRateLimitService CreateService(
        RegistrationEmailRateLimitOptions options = null,
        ICacheProvider cacheProvider = null)
    {
        var optionsSnapshot = new Mock<IOptionsSnapshot<RegistrationEmailRateLimitOptions>>();
        optionsSnapshot.Setup(x => x.Value).Returns(options ?? CreateOptions());

        return new RegistrationEmailRateLimitService(
            cacheProvider ?? new InMemoryCounterCacheProvider(),
            Mock.Of<ILogger<RegistrationEmailRateLimitService>>(),
            optionsSnapshot.Object);
    }

    private static TestRegistrationEmailRateLimitService CreateServiceWithUtcNowSequence(
        IEnumerable<DateTime> utcNowSequence,
        RegistrationEmailRateLimitOptions options,
        ICacheProvider cacheProvider)
    {
        var optionsSnapshot = new Mock<IOptionsSnapshot<RegistrationEmailRateLimitOptions>>();
        optionsSnapshot.Setup(x => x.Value).Returns(options);

        return new TestRegistrationEmailRateLimitService(
            utcNowSequence,
            cacheProvider,
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

        public Task HSetWithExpire(string key, string member, string value, TimeSpan? expire) => throw new NotImplementedException();
        public Task<bool> HashDeleteAsync(string key, string member) => throw new NotImplementedException();
        public Task<HashEntry[]> HGetAll(string key) => throw new NotImplementedException();
        public Task Set(string key, string value, TimeSpan? expire) => throw new NotImplementedException();
        public Task Set<T>(string key, T value, TimeSpan? expire) where T : class => throw new NotImplementedException();
        public Task<RedisValue> Get(string key) => throw new NotImplementedException();
        public Task<T> Get<T>(string key) where T : class => throw new NotImplementedException();
        public Task Delete(string key) => throw new NotImplementedException();
        public Task<Dictionary<string, RedisValue>> BatchGet(List<string> keys) => throw new NotImplementedException();
        public Task AddScoreAsync(string leaderboardKey, string member, double score) => throw new NotImplementedException();
        public Task<double> GetScoreAsync(string leaderboardKey, string member) => throw new NotImplementedException();
        public Task<long> GetRankAsync(string leaderboardKey, string member, bool highToLow = true) => throw new NotImplementedException();
        public Task<SortedSetEntry[]> GetTopAsync(string leaderboardKey, long startRank, long stopRank, bool highToLow = true) => throw new NotImplementedException();
        public Task<long> GetSortedSetLengthAsync(string leaderboardKey) => throw new NotImplementedException();
        public Task SetAddAsync(string key, string value, TimeSpan? timeSpan) => throw new NotImplementedException();
        public Task SetAddAsync(string key, List<string> values, TimeSpan? timeSpan) => throw new NotImplementedException();
        public Task SetRemoveAsync(string key, List<string> values) => throw new NotImplementedException();
        public Task<RedisValue[]> SetMembersAsync(string key) => throw new NotImplementedException();
    }

    private sealed class ThrowingCacheProvider : InMemoryCounterCacheProvider
    {
        public override Task<long> Increase(string key, int increase, TimeSpan? expire)
        {
            throw new InvalidOperationException("redis unavailable");
        }
    }

    private sealed class ThrowOnMatchedWindowCacheProvider : InMemoryCounterCacheProvider
    {
        private readonly string _windowMarker;
        private readonly int _throwOnMatchedCallNumber;
        private int _matchedCallCount;

        public ThrowOnMatchedWindowCacheProvider(string windowMarker, int throwOnMatchedCallNumber)
        {
            _windowMarker = windowMarker;
            _throwOnMatchedCallNumber = throwOnMatchedCallNumber;
        }

        public override Task<long> Increase(string key, int increase, TimeSpan? expire)
        {
            if (key.Contains(_windowMarker, StringComparison.Ordinal))
            {
                _matchedCallCount++;
                if (_matchedCallCount == _throwOnMatchedCallNumber)
                {
                    throw new InvalidOperationException("redis unavailable");
                }
            }

            return base.Increase(key, increase, expire);
        }
    }

    private sealed class RecordingCacheProvider : InMemoryCounterCacheProvider
    {
        public List<string> Keys { get; } = new();

        public override Task<long> Increase(string key, int increase, TimeSpan? expire)
        {
            Keys.Add(key);
            return base.Increase(key, increase, expire);
        }
    }

    private sealed class TestRegistrationEmailRateLimitService : RegistrationEmailRateLimitService
    {
        private readonly Queue<DateTime> _utcNowSequence;

        public TestRegistrationEmailRateLimitService(IEnumerable<DateTime> utcNowSequence, ICacheProvider cacheProvider,
            ILogger<RegistrationEmailRateLimitService> logger,
            IOptionsSnapshot<RegistrationEmailRateLimitOptions> options)
            : base(cacheProvider, logger, options)
        {
            _utcNowSequence = new Queue<DateTime>(utcNowSequence);
        }

        public int UtcNowCallCount { get; private set; }

        protected override DateTime GetUtcNow()
        {
            UtcNowCallCount++;
            return _utcNowSequence.Dequeue();
        }
    }
}
