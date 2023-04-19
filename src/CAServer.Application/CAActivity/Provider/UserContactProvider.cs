using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using CAServer.Entities.Es;
using JetBrains.Annotations;
using Nest;
using Volo.Abp.DependencyInjection;

namespace CAServer.CAActivity.Provider;

public interface IUserContactProvider
{
    [ItemCanBeNull]
    Task<List<Tuple<ContactAddress, string>>> BatchGetUserNameAsync(IEnumerable<string> usersAddresses, Guid userId,
        string chainId = null);
    
    Task<List<ContactAddress>> GetContactByUserNameAsync(string name, Guid userId);
}

public class UserContactProvider : IUserContactProvider, ISingletonDependency
{
    private readonly INESTRepository<ContactIndex, Guid> _contactIndexRepository;

    public UserContactProvider(INESTRepository<ContactIndex, Guid> contactIndexRepository)
    {
        _contactIndexRepository = contactIndexRepository;
    }

    public async Task<List<Tuple<ContactAddress, string>>> BatchGetUserNameAsync(IEnumerable<string> usersAddresses,
        Guid userId,
        string chainId = null)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<ContactIndex>, QueryContainer>>() { };
        mustQuery.Add(q => q.Terms(t => t.Field("addresses.address").Terms(usersAddresses)));
        if (chainId != null)
        {
            mustQuery.Add(q => q.Terms(t => t.Field("addresses.chainId").Terms(chainId)));
        }

        mustQuery.Add(q => q.Term(i => i.Field(f => f.UserId).Value(userId)));
        mustQuery.Add(q => q.Term(i => i.Field(f => f.IsDeleted).Value(false)));

        QueryContainer Filter(QueryContainerDescriptor<ContactIndex> f) => f.Bool(b => b.Must(mustQuery));
        var contactList = await _contactIndexRepository.GetListAsync(Filter);
        var ans = new List<Tuple<ContactAddress, string>>();
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
                ans.Add(new Tuple<ContactAddress, string>(address, contact.Name));
            }
        }

        return ans;
    }
    
    public async Task<List<ContactAddress>> GetContactByUserNameAsync(string name, Guid userId)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<ContactIndex>, QueryContainer>>() { };
        mustQuery.Add(q => q.Term(i => i.Field(f => f.UserId).Value(userId)));
        mustQuery.Add(q => q.Term(i => i.Field(f => f.Name).Value(name)));
        mustQuery.Add(q => q.Term(i => i.Field(f => f.IsDeleted).Value(false)));

        QueryContainer Filter(QueryContainerDescriptor<ContactIndex> f) => f.Bool(b => b.Must(mustQuery));
        var contact = await _contactIndexRepository.GetAsync(Filter);
        return contact?.Addresses;
    }
}