using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CAServer.CAAccount.Dtos;
using CAServer.Guardian;
using CAServer.Verifier.Dtos;

namespace CAServer.CAAccount;

public interface IGuardianUserProvider
{
    public Task<Tuple<string, string, bool>> GetSaltAndHashAsync(string guardianIdentifier, string guardianSalt, string poseidonHash);

    public Task AddGuardianAsync(string guardianIdentifier, string salt, string identifierHash, string poseidonHash);

    public Task AddUserInfoAsync(CAServer.Verifier.Dtos.UserExtraInfo userExtraInfo);

    public Task<bool> AppendGuardianPoseidonHashAsync(string guardianIdentifier, string identifierPoseidonHash);

    public Task<List<GuardianIndexDto>> GetGuardianListAsync(List<string> identifierHashList);

    public Task AppendSecondaryEmailInfo(VerifyTokenRequestDto requestDto, string guardianIdentifierHash,
        string guardianIdentifier, GuardianIdentifierType type);
}