using System;
using System.Threading.Tasks;
using AElf;
using AElf.CSharp.Core.Extension;
using AElf.Types;
using CAServer.Account;
using CAServer.CAAccount.Dtos;
using CAServer.CAAccount.Enums;
using CAServer.Common;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Logging;

namespace CAServer.CAAccount.Provider;

public class SignaturePreValidationProvider : CAServerAppService, IPreValidationStrategy
{
    private readonly IGetVerifierServerProvider _getVerifierServerProvider;
    private readonly ILogger<SignaturePreValidationProvider> _logger;
    
    public PreValidationType Type => PreValidationType.Signature;
    
    public SignaturePreValidationProvider(
        IGetVerifierServerProvider getVerifierServerProvider,
        ILogger<SignaturePreValidationProvider> logger)
    {
        _getVerifierServerProvider = getVerifierServerProvider;
        _logger = logger;
    }
    
    public bool ValidateParameters(GuardianInfo guardian)
    {
        return guardian is { VerificationInfo: not null }
               && !guardian.VerificationInfo.Id.IsNullOrEmpty()
               && !guardian.VerificationInfo.VerificationDoc.IsNullOrEmpty()
               && !guardian.VerificationInfo.Signature.IsNullOrEmpty();
    }

    public async Task<bool> PreValidateGuardian(string chainId, string caHash, string manager, GuardianInfo guardian)
    {
        return await CheckVerifierSignatureAndData(chainId, guardian, Hash.LoadFromHex(caHash), manager);
    }
    
    private async Task<bool> CheckVerifierSignatureAndData(string chainId, GuardianInfo guardianInfo, Hash caHash = null,
        string operationDetails = null)
    {
        var verifierDocLength = GetVerificationDocLength(guardianInfo.VerificationInfo.VerificationDoc);

        if (verifierDocLength != 7 && verifierDocLength != 8)
        {
            return false;
        }

        return await CheckVerifierSignatureAndDataWithCreateChainId(chainId, guardianInfo, caHash, operationDetails);
    }
    
    private int GetVerificationDocLength(string verificationDoc)
    {
        return string.IsNullOrWhiteSpace(verificationDoc) ? 0 : verificationDoc.Split(",").Length;
    }
    
    private VerificationDocInfo GetVerificationDoc(string doc)
    {
        var docs = doc.Split(",");
        return new VerificationDocInfo
        {
            GuardianType = docs[0],
            IdentifierHash = Hash.LoadFromHex(docs[1]),
            VerificationTime = docs[2],
            VerifierAddress = Address.FromBase58(docs[3]),
            Salt = docs[4],
            OperationType = docs[5]
        };
    }

    private async Task<bool> CheckVerifierSignatureAndDataWithCreateChainId(string chainId, GuardianInfo guardianInfo,
        Hash caHash, string operationDetails = null)
    {
        //[type,guardianIdentifierHash,verificationTime,verifierAddress,salt,operationType,createChainId<,operationHash>]
        var verificationDoc = guardianInfo.VerificationInfo.VerificationDoc;
        if (verificationDoc == null || string.IsNullOrWhiteSpace(verificationDoc))
        {
            return false;
        }

        var verifierDoc = verificationDoc.Split(",");

        var docInfo = GetVerificationDoc(verificationDoc);

        if (docInfo.OperationType == "0")
        {
            return false;
        }

        // var key = HashHelper.ComputeFrom(guardianInfo.Signature.ToByteArray());
        // if (State.VerifierDocMap[key])
        // {
        //     return false;
        // }

        //Check expired time 1h.
        var verificationTime = DateTime.SpecifyKind(Convert.ToDateTime(docInfo.VerificationTime), DateTimeKind.Utc);
        if (verificationTime.ToTimestamp().AddHours(1) <= Timestamp.FromDateTimeOffset(DateTimeOffset.UtcNow) ||
            !int.TryParse(docInfo.GuardianType, out var type) ||
            (int)guardianInfo.Type != type ||
            guardianInfo.IdentifierHash != docInfo.IdentifierHash.ToHex())
        {
            return false;
        }

        var operationTypeStr = docInfo.OperationType;
        var operationTypeName = typeof(OperationTypeInContract).GetEnumName(Convert.ToInt32(operationTypeStr))?.ToLower();
        if (operationTypeName != nameof(OperationTypeInContract.SocialRecovery).ToLower())
        {
            return false;
        }

        //Check verifier address and data.
        var verifierAddress = docInfo.VerifierAddress;
        var verifierServerInfo = await _getVerifierServerProvider.GetVerifierServerAsync(guardianInfo.VerificationInfo.Id, chainId);
        if (verifierServerInfo == null || !verifierServerInfo.VerifierAddresses.Contains(verifierAddress.ToBase58()))
        {
            return false;
        }

        // var verifierAddressFromPublicKey =
        //     RecoverVerifierAddress(guardianInfo.VerificationDoc, ByteStringHelper.FromHexString(guardianInfo.Signature));
        // if (verifierAddressFromPublicKey != verifierAddress)
        // {
        //     verifierAddressFromPublicKey =
        //         RecoverVerifierAddress($"{guardianInfo.VerificationDoc},{operationDetails}", ByteStringHelper.FromHexString(guardianInfo.Signature));
        // }
        // if (verifierAddressFromPublicKey != verifierAddress)
        // {
        //     return false;
        // }

        if (!CheckVerifierSignatureOperationDetail(operationTypeName, verifierDoc, operationDetails))
        {
            return false;
        }
        // State.VerifierDocMap[key] = true;

        //After verifying the contents of the operation,it is not necessary to verify the 'ChainId'
        if (verifierDoc.Length >= 8 &&
            ((operationTypeName == nameof(OperationTypeInContract.CreateCAHolder).ToLower() && caHash != null) ||
             operationTypeName == nameof(OperationTypeInContract.SocialRecovery).ToLower()))
        {
            return true;
        }

        return int.Parse(verifierDoc[6]) == ChainHelper.ConvertBase58ToChainId(chainId);
    }
    
    // private Address RecoverVerifierAddress(string verificationDoc, ByteString signature)
    // {
    //     var data = HashHelper.ComputeFrom(verificationDoc);
    //     var publicKey = Context.RecoverPublicKey(signature.ToByteArray(),
    //         data.ToByteArray());
    //     return Address.FromPublicKey(publicKey);
    // }
    
    private bool CheckVerifierSignatureOperationDetail(string operationTypeName, string[] verifierDoc,
        string operationDetails)
    {
        if (verifierDoc.Length >= 8)
        {
            if (verifierDoc.Length < 8 || string.IsNullOrWhiteSpace(verifierDoc[7]) ||
                string.IsNullOrWhiteSpace(operationDetails))
            {
                return false;
            }

            return verifierDoc[7] == HashHelper.ComputeFrom(operationDetails).ToHex();
        }

        return true;
    }
}