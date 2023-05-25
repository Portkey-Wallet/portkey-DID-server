using System.Threading.Tasks;
using CAServer.Common;
using CAServer.Options;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;

namespace CAServer.Notify.Provider;

public class NotifyProvider : INotifyProvider, ISingletonDependency
{
    private readonly IHttpClientService _httpClientService;
    private readonly CmsConfigOptions _cmsConfigOptions;

    public NotifyProvider(IHttpClientService httpClientService,
        IOptionsSnapshot<CmsConfigOptions> cmsConfigOptions)
    {
        _httpClientService = httpClientService;
        _cmsConfigOptions = cmsConfigOptions.Value;
    }

    public async Task<T> GetDataFromCms<T>(string condition)
    {
        var url = $"{_cmsConfigOptions.Uri}{condition}&access_token={_cmsConfigOptions.AccessToken}";
        return await _httpClientService.GetAsync<T>(url);
    }
}