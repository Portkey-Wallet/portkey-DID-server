using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CAServer.Guardian;

namespace CAServer.CAAccount;

public interface IGuardianUserProvider
{
    public Task<Tuple<string, string, bool>> GetSaltAndHashAsync(string guardianIdentifier, string guardianIdentifierHash, string guardianSalt, string poseidonHash);

    public Task AddGuardianAsync(string guardianIdentifier, string salt, string identifierHash, string poseidonHash);

    public Task AddUserInfoAsync(CAServer.Verifier.Dtos.UserExtraInfo userExtraInfo);

    public Task<bool> AppendGuardianPoseidonHashAsync(string guardianIdentifier, string identifierPoseidonHash);

    public Task<List<GuardianIndexDto>> GetGuardianListAsync(List<string> identifierHashList);
}