using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using AElf.LinqToElasticSearch.Provider;
using CAServer.Entities.Es;
using Google.Protobuf.WellKnownTypes;
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
    private readonly ILinqRepository<ContactIndex, Guid> _contactIndexRepository;

    public UserContactProvider(ILinqRepository<ContactIndex, Guid> contactIndexRepository)
    {
        _contactIndexRepository = contactIndexRepository;
    }

    public async Task<List<Tuple<ContactAddress, string>>> BatchGetUserNameAsync(IEnumerable<string> usersAddresses,
        Guid userId,
        string chainId = null)
    {
        Expression<Func<ContactIndex, bool>> mustQuery = p => p.UserId == userId && p.IsDeleted == false;
        if (chainId != null)
        {
            mustQuery =  mustQuery.And(p=>p.Addresses.Any(i=>i.ChainId == chainId));
        }
        
        Expression<Func<ContactIndex, bool>> shouldQuery = null;
        foreach (var usersAddress in usersAddresses) {
            shouldQuery = shouldQuery is null ? (p => p.Addresses.Any(i => i.Address == usersAddress)) :shouldQuery.Or(p => p.Addresses.Any(i => i.Address == usersAddress));
        }
        
        mustQuery = mustQuery.And(shouldQuery);
        
        var contactList = _contactIndexRepository.WhereClause(mustQuery).Skip(0).Take(1000).ToList();
        
        var ans = new List<Tuple<ContactAddress, string>>();
        if (contactList.Count <= 0)
        {
            return ans;
        }

        foreach (var contact in contactList)
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
        Expression<Func<ContactIndex, bool>> expression = p => p.UserId == userId && p.Name == name && !p.IsDeleted;
        var contact = _contactIndexRepository.WhereClause(expression).Skip(0).Take(1000).ToList();
        return contact?.FirstOrDefault()?.Addresses;
    }
}