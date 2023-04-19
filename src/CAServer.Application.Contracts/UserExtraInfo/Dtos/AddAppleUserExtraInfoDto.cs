using System.ComponentModel.DataAnnotations;

namespace CAServer.UserExtraInfo.Dtos;

public class AddAppleUserExtraInfoDto
{
    [Required] public string IdentityToken { get; set; }
    [Required] public AppleUserInfoDto UserInfo { get; set; }
}

public class AppleUserInfoDto
{
    [Required] public AppleUserName Name { get; set; }
    [Required] public string Email { get; set; }
}

public class AppleUserName
{
    [Required] public string FirstName { get; set; }
    [Required] public string LastName { get; set; }
}