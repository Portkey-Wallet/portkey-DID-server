using System.Collections.Generic;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;

namespace CAServer.CAAccount.Provider;

public interface IUserProfilePictureProvider
{
    public List<string> GetDefaultUserPictures();
}

public class UserProfilePictureProvider : IUserProfilePictureProvider, ISingletonDependency
{
    private readonly IOptionsMonitor<UserProfilePictureOptions> _userProfilePictureOptions;
    
    public UserProfilePictureProvider(IOptionsMonitor<UserProfilePictureOptions> userProfilePictureOptions)
    {
        _userProfilePictureOptions = userProfilePictureOptions;
    }
    
    public List<string> GetDefaultUserPictures()
    {
        return _userProfilePictureOptions.CurrentValue.Pictures;
    }
}