using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CAServer.ContactClean.Provider;
using CAServer.Contacts.Provider;
using CAServer.Entities.Es;
using CAServer.Search;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Auditing;

namespace CAServer.ContactClean;

[DisableAuditing, RemoteService(false)]
public class ContactCleanAppService : CAServerAppService, IContactCleanAppService
{
    private readonly IContactProvider _contactProvider;
    private readonly IContactCleanProvider _contactCleanProvider;
    private readonly IEnumerable<ISearchService> _esServices;

    private static PagedResultDto<UserIndex> _pagedResultDto;

    public ContactCleanAppService(IContactProvider contactProvider, IEnumerable<ISearchService> esServices,
        IContactCleanProvider contactCleanProvider)
    {
        _contactProvider = contactProvider;
        _esServices = esServices;
        _contactCleanProvider = contactCleanProvider;

        GetPagedResultDto().Wait();
    }

    public async Task<string> ContactCleanAsync(Guid userId)
    {
        // get wallet name of userId
        var holders = await _contactProvider.GetCaHoldersAsync(new List<Guid>() { userId });

        var holder = holders?.FirstOrDefault();
        if (holder == null)
        {
            Logger.LogError("get holder fail, userId:{userId}", userId.ToString());
        }

        var imUser = _pagedResultDto.Items.FirstOrDefault(t => t.Id == userId);

        if (imUser == null) throw new UserFriendlyException("user not register relation one.");

        // modify own walletName
        await SetNameAsync(imUser.RelationId, holder.NickName);

        // modify added contacts
        // 1.follow, 2.remark
        await ModifyAddedContacts(userId, imUser.RelationId);

        return "ok";
    }

    public async Task<int> ContactCleanAsync()
    {
        var successCount = 0;
        foreach (var user in _pagedResultDto.Items)
        {
            try
            {
                var holder = await GetHolderAsync(user.Id);
                if (holder == null)
                {
                    continue;
                }

                await SetNameAsync(user.RelationId, holder.NickName);
                await ModifyAddedContacts(user.Id, user.RelationId);
                successCount++;
            }
            catch (Exception e)
            {
                Logger.LogError(e, "[ContactCleanAllAsync] error, userInfo: {userInfo}",
                    JsonConvert.SerializeObject(user));
            }
        }

        return successCount;
    }

    private async Task<CAHolderIndex> GetHolderAsync(Guid userId)
    {
        var holders = await _contactProvider.GetCaHoldersAsync(new List<Guid>() { userId });
        return holders?.FirstOrDefault();
    }

    private async Task SetNameAsync(string relationId, string walletName)
    {
        await _contactCleanProvider.SetNameAsync(relationId, walletName);
    }

    private async Task ModifyAddedContacts(Guid userId, string relationId)
    {
        // get contacts be added
        var contacts = await _contactProvider.GetContactsAsync(userId);

        contacts = contacts?.Where(t => t.ImInfo != null && !t.ImInfo.RelationId.IsNullOrWhiteSpace())
            .ToList();
        if (contacts == null || contacts.Count == 0)
        {
            Logger.LogInformation("no contact with imUserInfo userId: {userId}", userId);
        }

        // follow and modify contact name of be added
        foreach (var contact in contacts)
        {
            await _contactCleanProvider.FollowAndRemarkAsync(relationId, contact.ImInfo.RelationId,
                contact.Name ?? string.Empty);

            await Task.Delay(100);
        }
    }

    private async Task GetPagedResultDto()
    {
        var imUsersStr =
            await GetListByLucenceAsync("userindex",
                new GetListInput() { MaxResultCount = 1000, SkipCount = 0 });

        if (imUsersStr.IsNullOrWhiteSpace()) throw new UserFriendlyException("get user from es returns empty");
        var pageResult = JsonConvert.DeserializeObject<PagedResultDto<UserIndex>>(imUsersStr);
        if (pageResult.TotalCount == 0) throw new UserFriendlyException("get im user fail.");

        _pagedResultDto = pageResult;
    }

    public async Task<string> GetListByLucenceAsync(string indexName, GetListInput input)
    {
        try
        {
            var index = $"im.{indexName}";

            var esService = _esServices.FirstOrDefault(e => e.IndexName == index);
            if (input.MaxResultCount > 1000)
            {
                input.MaxResultCount = 1000;
            }

            return esService == null ? null : await esService.GetListByLucenceAsync(index, input);
        }
        catch (Exception e)
        {
            Logger.LogError("Search from es error.", e);
            throw;
        }
    }
}