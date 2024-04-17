using System;
using System.Threading.Tasks;
using CAServer.CAAccount;

namespace CAServer.VerifyToken;

public interface IVerifyTokenStrategy
{
    string Type { get; }

    Task<bool> VerifyRevokeToken(RevokeAccountInput input);

}