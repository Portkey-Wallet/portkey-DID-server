using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using CAServer.Common;
using CAServer.Commons;
using CAServer.Options;
using CAServer.Transfer.Dtos;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;

namespace CAServer.Transfer.Proxy;

public interface IETransferClientProvider
{
    Task<ResponseWrapDto<T>> GetAsync<T>(string uri);
    Task<ResponseWrapDto<T>> GetAsync<T>(string uri, object requestParam);
    Task<T> PostFormAsync<T>(string uri, object requestParam);
}

public class ETransferClientProvider : IETransferClientProvider, ISingletonDependency
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IOptionsMonitor<ETransferOptions> _options;
    private readonly IHttpClientService _httpClientService;
    private readonly ILogger<ETransferClientProvider> _logger;

    public ETransferClientProvider(IHttpContextAccessor httpContextAccessor, IOptionsMonitor<ETransferOptions> options,
        IHttpClientService httpClientService, ILogger<ETransferClientProvider> logger)
    {
        _httpContextAccessor = httpContextAccessor;
        _options = options;
        _httpClientService = httpClientService;
        _logger = logger;
    }

    public async Task<ResponseWrapDto<T>> GetAsync<T>(string uri)
    {
        try
        {
            return await _httpClientService.GetAsync<ResponseWrapDto<T>>(GetUrl(uri), GetHeader());
        }
        catch (Exception e)
        {
            if (e is HttpRequestException { StatusCode: HttpStatusCode.Unauthorized })
            {
                return new ResponseWrapDto<T>()
                {
                    Code = "40001",
                    Message = "UnAuthorized"
                };
            }

            _logger.LogError(e, "get error, uri:{uri}", uri);
            throw;
        }
    }

    public async Task<ResponseWrapDto<T>> GetAsync<T>(string uri, object requestParam)
    {
        return await GetAsync<T>(uri);
    }

    public async Task<T> PostFormAsync<T>(string uri, object requestParam)
    {
        var param = GetFormParam(requestParam);
        return await _httpClientService.PostAsync<T>(GetUrl(uri), RequestMediaType.Form, param, GetHeader());
    }

    private Dictionary<string, string> GetHeader()
    {
        var headers = new Dictionary<string, string>();
        var authToken = _httpContextAccessor.HttpContext?.Request.Headers.GetOrDefault(ETransferConstant.AuthHeader)
            .FirstOrDefault();
        if (!authToken.IsNullOrEmpty())
        {
            headers.Add(CommonConstant.AuthHeader, authToken);
        }

        if (!_options.CurrentValue.Version.IsNullOrEmpty())
        {
              headers.Add(CommonConstant.VersionName, _options.CurrentValue.Version);
        }

        return headers;
    }

    private Dictionary<string, string> GetFormParam(object requestParam)
    {
        if (requestParam is Dictionary<string, string> param) return param;

        var propertyInfos = requestParam.GetType().GetProperties();
        var dicParam = new Dictionary<string, string>();
        foreach (var propertyInfo in propertyInfos)
        {
            var val = propertyInfo.GetValue(requestParam, null);
            if (val == null) continue;

            var key = propertyInfo.Name;
            var attr = propertyInfo.GetCustomAttribute(typeof(FromFormAttribute));
            if (attr != null)
            {
                key = ((FromFormAttribute)attr).Name;
            }

            dicParam.Add(key, val.ToString());
        }

        return dicParam;
    }

    private string GetUrl(string uri)
    {
        var url = uri.StartsWith(CommonConstant.ProtocolName)
            ? uri
            : $"{_options.CurrentValue.BaseUrl.TrimEnd('/')}/{_options.CurrentValue.Prefix}/{uri.TrimStart('/')}";

        var queryString = _httpContextAccessor.HttpContext?.Request.QueryString;
        if (queryString.HasValue && !url.Contains("?"))
        {
            url += queryString.Value;
        }

        return url;
    }
}