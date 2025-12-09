using System;
using System.Collections.Generic;
using Xunit;

namespace CAServer.CAActivity.Provider;

[Collection(CAServerTestConsts.CollectionDefinitionName)]
public class UserContactProviderTests : CAServerApplicationTestBase
{
    private IUserContactProvider _userContactProvider;
    
    public UserContactProviderTests()
    {
        _userContactProvider = GetRequiredService<IUserContactProvider>();
    }
    
    [Fact]
    public async void GetUserContactAsyncTest()
    {
        var result = await _userContactProvider.BatchGetUserNameAsync(new List<string>
        {
           "address1", "address2"
        }, Guid.NewGuid(), "123");
    }
    
    [Fact]
    public async void GetUserContactListAsyncTest()
    {
        var result = await _userContactProvider.GetContactByUserNameAsync("123", Guid.NewGuid(),"");
    }
    
}