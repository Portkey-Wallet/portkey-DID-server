using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CAServer.Common;
using CAServer.Contacts;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Volo.Abp;
using Volo.Abp.DependencyInjection;

namespace CAServer.ContactClean.Provider;

public class ContactCleanProvider : IContactCleanProvider, ISingletonDependency
{
    private readonly IHttpClientService _httpClientService;
    private readonly ILogger<ContactCleanProvider> _logger;
    private readonly RelationOneOptions _relationOneOptions;

    public ContactCleanProvider(IHttpClientService httpClientService, ILogger<ContactCleanProvider> logger,
        IOptionsSnapshot<RelationOneOptions> relationOneOptions)
    {
        _httpClientService = httpClientService;
        _logger = logger;
        _relationOneOptions = relationOneOptions.Value;
    }

    public async Task FollowAndRemarkAsync(string relationId, string followRelationId, string name)
    {
        await FollowAsync(relationId, followRelationId);
        await RemarkAsync(relationId, followRelationId, name);
    }

    public async Task SetNameAsync(string relationId, string name)
    {
        try
        {
            if (name.IsNullOrWhiteSpace())
            {
                _logger.LogError("[SetNameAsync] fail, name is empty");
                return;
            }

            if (relationId.IsNullOrWhiteSpace())
            {
                _logger.LogError("[SetNameAsync] fail, relationId is empty");
                return;
            }

            var url = $"{RelationOneConstant.FlushModifyNameUrl}?relationId={relationId}";

            var result = await PostAsync<RelationOneResponseDto>(url, new { name = name });
            if (result.Code != RelationOneConstant.SuccessCode)
            {
                throw new UserFriendlyException($"modify name fail, {result.Desc}");
            }

            _logger.LogInformation("modify name success: relationId:{relationId}, name:{name}", relationId, name);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "follow error, address: ");
        }
    }

    private async Task FollowAsync(string relationId, string followRelationId)
    {
        try
        {
            if (relationId.IsNullOrWhiteSpace() || followRelationId.IsNullOrWhiteSpace()) return;

            var url = $"{RelationOneConstant.FlushFollowUrl}?relationId={relationId}";

            var result = await PostAsync<RelationOneResponseDto>(url, new { address = followRelationId });
            if (result.Code != RelationOneConstant.SuccessCode)
            {
                _logger.LogError("{relationId} follow {followRelationId} fail, message: {message}", relationId,
                    followRelationId, result.Desc);
                throw new UserFriendlyException($"follow fail, {result.Desc}");
            }

            _logger.LogInformation("{relationId} follow {followRelationId} success", relationId, followRelationId);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "{relationId} follow {followRelationId} error", relationId, followRelationId);
        }
    }

    private async Task RemarkAsync(string relationId, string followRelationId, string name)
    {
        try
        {
            if (relationId.IsNullOrWhiteSpace() || followRelationId.IsNullOrWhiteSpace()) return;

            var url = $"{RelationOneConstant.FlushRemarkUrl}?relationId={relationId}";

            var result =
                await PostAsync<RelationOneResponseDto>(url, new { relationId = followRelationId, name = name });
            if (result.Code != RelationOneConstant.SuccessCode)
            {
                _logger.LogError("{relationId} remark {followRelationId} fail, message: {message}", relationId,
                    followRelationId, result.Desc);
                
                throw new UserFriendlyException($"remark name fail, {result.Desc}");
            }

            _logger.LogInformation("{relationId} remark {followRelationId} success, name: {name}", relationId,
                followRelationId, name);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "{relationId} remark {followRelationId} error, name: {name}", relationId,
                followRelationId, name);
        }
    }

    private async Task<T> PostAsync<T>(string url, object paramObj) where T : class
    {
        var requestInfo = GetRequestInfo(url);
        return await _httpClientService.PostAsync<T>(requestInfo.reqUrl, paramObj, requestInfo.header);
    }

    private (string reqUrl, Dictionary<string, string> header) GetRequestInfo(string url)
    {
        return (reqUrl: GetUrl(url), header: GetDefaultHeader());
    }

    private string GetUrl(string url)
    {
        if (!_relationOneOptions.UrlPrefix.IsNullOrWhiteSpace())
        {
            return $"{_relationOneOptions.BaseUrl.TrimEnd('/')}/{_relationOneOptions.UrlPrefix.TrimEnd('/')}/{url}";
        }

        return $"{_relationOneOptions.BaseUrl.TrimEnd('/')}/{url}";
    }

    private Dictionary<string, string> GetDefaultHeader() => new Dictionary<string, string>
        { ["ApiKey"] = _relationOneOptions.ApiKey };
}