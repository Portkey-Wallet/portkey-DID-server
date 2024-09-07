using System.Text.Json.Serialization;

namespace TonProof.Types;

/// <summary>
/// Represents an object for proof verification.
/// <see href="https://docs.ton.org/develop/dapps/ton-connect/sign#structure-of-ton_proof"/>
/// <seealso href="https://github.com/ton-connect/demo-dapp-backend?tab=readme-ov-file"/>
/// </summary>
public class CheckProofRequest
{
    [JsonPropertyName("address")]
    public string Address { get; set; }
    
    [JsonPropertyName("network")]
    public string Network { get; set; }
    
    [JsonPropertyName("public_key")]
    public string PublicKey { get; set; }
    
    [JsonPropertyName("proof")]
    public Proof Proof { get; set; }
}

public record Proof
{
    [JsonPropertyName("timestamp")]
    public long Timestamp { get; set; }
    
    [JsonPropertyName("domain")]
    public Domain Domain { get; set; }
    
    [JsonPropertyName("signature")]
    public string Signature { get; set; }
    
    [JsonPropertyName("payload")]
    public string Payload { get; set; }
    
    [JsonPropertyName("state_init")]
    public string StateInit { get; set; }
}

public record Domain
{
    [JsonPropertyName("LengthBytes")]
    public uint LengthBytes { get; set; }

    [JsonPropertyName("value")]
    public string Value { get; set; }
}