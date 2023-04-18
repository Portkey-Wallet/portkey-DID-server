using AutoMapper;
using CAServer.Tokens;

namespace CAServer.Entities.Etos;

[AutoMap(typeof(UserToken))]
public class UserTokenEto : UserToken
{
    
}