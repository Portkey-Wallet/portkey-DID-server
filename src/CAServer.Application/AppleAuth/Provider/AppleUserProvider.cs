using System.Threading.Tasks;
using CAServer.Options;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using StackExchange.Redis;
using Volo.Abp.DependencyInjection;

namespace CAServer.AppleAuth.Provider;

public class AppleUserProvider : IAppleUserProvider, ISingletonDependency
{
    private IDatabase Db { get; set; }

    private const string Key = "AppleUserExtraInfo";

    public AppleUserProvider(IOptions<AppleCacheOptions> cacheOptions)
    {
        var redisConnection = ConnectionMultiplexer.Connect(cacheOptions.Value.Configuration);
        Db = redisConnection.GetDatabase(cacheOptions.Value.Db);
    }

    public async Task SetUserExtraInfoAsync(AppleUserExtraInfo userExtraInfo)
    {
        await Db.HashSetAsync(Key, userExtraInfo.UserId, JsonConvert.SerializeObject(userExtraInfo));
    }

    public async Task<AppleUserExtraInfo> GetUserExtraInfoAsync(string userId)
    {
        var userInfo = await Db.HashGetAsync(Key, userId);

        if (!userInfo.HasValue) return null;
        return JsonConvert.DeserializeObject<AppleUserExtraInfo>(userInfo);
    }

    public async Task<bool> UserExtraInfoExistAsync(string userId) => await Db.HashExistsAsync(Key, userId);
}