using System;
using System.Threading.Tasks;

namespace CAServer.CAAccount;

public interface IGuardianUserProvider
{
    public Task<Tuple<string, string, bool>> GetSaltAndHashAsync(string guardianIdentifier, string guardianIdentifierHash, string guardianSalt);

    public Task AddGuardianAsync(string guardianIdentifier, string salt, string identifierHash);

    public Task AddUserInfoAsync(CAServer.Verifier.Dtos.UserExtraInfo userExtraInfo);

    public Task<bool> AppendGuardianPoseidonHashAsync(string guardianIdentifier, string identifierPoseidonHash);
}