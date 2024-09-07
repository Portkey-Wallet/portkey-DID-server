namespace TonProof.Types;

/// <summary>
/// Represents the result of the proof verification process.
/// </summary>
public enum VerifyResult
{
    /// <summary>
    /// The proof is valid and the verification was successful.
    /// </summary>
    Valid = 1,

    /// <summary>
    /// The hash derived from the proof does not match the expected hash, indicating a verification failure.
    /// </summary>
    HashMismatch = -1,

    /// <summary>
    /// The domain specified in the proof is not allowed.
    /// </summary>
    DomainNotAllowed = -2,

    /// <summary>
    /// The address in the proof does not match the expected address.
    /// </summary>
    AddressMismatch = -3,

    /// <summary>
    /// The public key provided in the proof does not match the expected public key.
    /// </summary>
    PublicKeyMismatch = -4,

    /// <summary>
    /// The proof has expired and is no longer valid.
    /// </summary>
    ProofExpired = -5,

    /// <summary>
    /// Invalid InitState structure.
    /// </summary>
    InvalidInitState = -6,
    
    /// <summary>
    /// The address lacks the correct format and omits a workchain.
    /// </summary>
    InvalidAddress = -7
}