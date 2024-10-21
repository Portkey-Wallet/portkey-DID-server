using System.Collections.Generic;
using System.Linq;

namespace CAServer.CAAccount.Provider.TonWallet;

/// <summary>
/// Provides configuration options for proof verification.
/// </summary>
public class TonProofOptions
{
    /// <summary>
    /// The prefix used in TonConnect for identification.
    /// </summary>
    public string TonConnectPrefix { get; set; } = "ton-connect";

    /// <summary>
    /// The prefix used for proof items in the system.
    /// </summary>
    public string TonProofPrefix { get; set; } = "ton-proof-item-v2/";

    /// <summary>
    /// Maximum allowed time (in seconds) for a proof to be considered valid.
    /// Default is 15 minutes (900 seconds).
    /// </summary>
    public long ValidAuthTime { get; set; } = 15 * 60; // 15 minutes

    /// <summary>
    /// A collection of allowed domains that are considered valid for proof verification.
    /// </summary>
    public IEnumerable<string> AllowedDomains { get; set; } = Enumerable.Empty<string>();

    /// <summary>
    /// A dictionary mapping known wallet contract codes to their corresponding creation functions.
    /// </summary>
    // public Dictionary<string, Func<IWalletContract>> KnownWallets { get; set; } = new()
    // {
    //     { WalletContractV1R1.CodeBase64, WalletContractV1R1.Create },
    //     { WalletContractV1R2.CodeBase64, WalletContractV1R2.Create },
    //     { WalletContractV1R3.CodeBase64, WalletContractV1R3.Create },
    //
    //     { WalletContractV2R1.CodeBase64, WalletContractV2R1.Create },
    //     { WalletContractV2R2.CodeBase64, WalletContractV2R2.Create },
    //
    //     { WalletContractV3R1.CodeBase64, WalletContractV3R1.Create },
    //     { WalletContractV3R2.CodeBase64, WalletContractV3R2.Create },
    //
    //     { WalletContractV4R2.CodeBase64, WalletContractV4R2.Create }
    // };
}