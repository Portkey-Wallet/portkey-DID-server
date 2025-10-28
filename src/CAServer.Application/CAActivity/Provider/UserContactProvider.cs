using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using CAServer.Commons;
using CAServer.Entities.Es;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using Nest;
using Volo.Abp.DependencyInjection;

namespace CAServer.CAActivity.Provider;

public interface IUserContactProvider
{
    [ItemCanBeNull]
    Task<List<Tuple<ContactAddress, string, string>>> BatchGetUserNameAsync(List<string> usersAddresses,
        Guid userId, string version, string chainId = null);

    Task<List<ContactAddress>> GetContactByUserNameAsync(string name, Guid userId, string version);
}

public class UserContactProvider : IUserContactProvider, ISingletonDependency
{
    private readonly INESTRepository<ContactIndex, Guid> _contactIndexRepository;
    private readonly INESTRepository<AddressBookIndex, Guid> _addressBookRepository;
    private readonly ILogger<UserContactProvider> _logger;

    public UserContactProvider(INESTRepository<ContactIndex, Guid> contactIndexRepository,
        INESTRepository<AddressBookIndex, Guid> addressBookRepository, ILogger<UserContactProvider> logger)
    {
        _contactIndexRepository = contactIndexRepository;
        _addressBookRepository = addressBookRepository;
        _logger = logger;
    }

    public async Task<List<Tuple<ContactAddress, string, string>>> BatchGetUserNameAsync(
        List<string> usersAddresses,
        Guid userId, string version,
        string chainId = null)
    {
        try
        {
            return VersionContentHelper.CompareVersion(version, CommonConstant.RevampVersion)
                ? await BatchGetAddressBookNameAsync(usersAddresses, userId, chainId)
                : await BatchGetContactNameAsync(usersAddresses, userId, chainId);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "BatchGetUserName error, message:{message}, stack:{stack}", e.Message,
                e.StackTrace ?? "-");
            return new List<Tuple<ContactAddress, string, string>>();
        }
    }

    public async Task<List<ContactAddress>> GetContactByUserNameAsync(string name, Guid userId, string version)
    {
        try
        {
            return VersionContentHelper.CompareVersion(version, CommonConstant.RevampVersion)
                ? await GetAddressBookByUserNameAsync(name, userId)
                : await GetContactByUserNameAsync(name, userId);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "GetContactByUserName error, message:{message}, stack:{stack}", e.Message,
                e.StackTrace ?? "-");
            return new List<ContactAddress>();
        }
    }

    private async Task<List<Tuple<ContactAddress, string, string>>> BatchGetContactNameAsync(
        List<string> usersAddresses,
        Guid userId,
        string chainId = null)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<ContactIndex>, QueryContainer>>() { };
        if (!usersAddresses.IsNullOrEmpty())
        {
            mustQuery.Add(q => q.Terms(t => t.Field("addresses.address").Terms(usersAddresses)));
        }

        if (!chainId.IsNullOrEmpty())
        {
            mustQuery.Add(q => q.Terms(t => t.Field("addresses.chainId").Terms(chainId)));
        }

        mustQuery.Add(q => q.Term(i => i.Field(f => f.UserId).Value(userId)));
        mustQuery.Add(q => q.Term(i => i.Field(f => f.IsDeleted).Value(false)));

        QueryContainer Filter(QueryContainerDescriptor<ContactIndex> f) => f.Bool(b => b.Must(mustQuery));
        var contactList = await _contactIndexRepository.GetListAsync(Filter);
        var ans = new List<Tuple<ContactAddress, string, string>>();
        if (contactList?.Item2 == null)
        {
            return ans;
        }

        foreach (var contact in contactList.Item2)
        {
            if (contact?.Addresses == null)
            {
                continue;
            }

            foreach (var address in contact?.Addresses)
            {
                ans.Add(new Tuple<ContactAddress, string, string>(address,
                    contact.Name.IsNullOrWhiteSpace() ? contact.CaHolderInfo?.WalletName : contact.Name,
                    contact.Avatar));
            }
        }

        return ans;
    }

    public async Task<List<ContactAddress>> GetContactByUserNameAsync(string name, Guid userId)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<ContactIndex>, QueryContainer>>() { };
        mustQuery.Add(q => q.Term(i => i.Field(f => f.UserId).Value(userId)));
        mustQuery.Add(q => q.Term(i => i.Field(f => f.IsDeleted).Value(false)));

        QueryContainer Filter(QueryContainerDescriptor<ContactIndex> f) => f.Bool(b => b.Must(mustQuery));
        var contacts = await _contactIndexRepository.GetListAsync(Filter);
        var contactList = contacts.Item2.Where(t => t.Name == name || t.CaHolderInfo?.WalletName == name).ToList();

        return contactList.SelectMany(t => t.Addresses).ToList();
    }

    private async Task<List<ContactAddress>> GetAddressBookByUserNameAsync(string name, Guid userId)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<AddressBookIndex>, QueryContainer>>() { };
        mustQuery.Add(q => q.Term(i => i.Field(f => f.UserId).Value(userId)));
        mustQuery.Add(q => q.Term(i => i.Field(f => f.Name).Value(name)));
        mustQuery.Add(q => q.Term(i => i.Field(f => f.IsDeleted).Value(false)));

        QueryContainer Filter(QueryContainerDescriptor<AddressBookIndex> f) => f.Bool(b => b.Must(mustQuery));
        var (count, contactList) = await _addressBookRepository.GetListAsync(Filter);
        return contactList.Select(t => new ContactAddress
        {
            ChainId = t.AddressInfo?.ChainId,
            ChainName = t.AddressInfo?.Network,
            Address = t.AddressInfo?.Address
        }).ToList();
    }

    private async Task<List<Tuple<ContactAddress, string, string>>> BatchGetAddressBookNameAsync(
        List<string> usersAddresses,
        Guid userId, string chainId = null)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<AddressBookIndex>, QueryContainer>>() { };
        if (!usersAddresses.IsNullOrEmpty())
        {
            mustQuery.Add(q => q.Terms(t => t.Field(f => f.AddressInfo.Address).Terms(usersAddresses)));
        }

        if (!chainId.IsNullOrEmpty())
        {
            mustQuery.Add(q => q.Term(t => t.Field(f => f.AddressInfo.ChainId).Value(chainId)));
        }

        mustQuery.Add(q => q.Term(i => i.Field(f => f.UserId).Value(userId)));
        mustQuery.Add(q => q.Term(i => i.Field(f => f.IsDeleted).Value(false)));

        QueryContainer Filter(QueryContainerDescriptor<AddressBookIndex> f) => f.Bool(b => b.Must(mustQuery));
        var (count, contactList) = await _addressBookRepository.GetListAsync(Filter);
        var ans = new List<Tuple<ContactAddress, string, string>>();
        if (contactList.IsNullOrEmpty())
        {
            return ans;
        }

        foreach (var contact in contactList)
        {
            if (contact?.AddressInfo == null)
            {
                continue;
            }

            var addressInfo = new ContactAddress
            {
            };

            ans.Add(new Tuple<ContactAddress, string, string>(addressInfo, contact.Name,
                contact.CaHolderInfo == null ? string.Empty : contact.CaHolderInfo.Avatar));
        }

        return ans;
    }
}