using System.Text.Json.Serialization;

namespace TonProof.Types;

internal sealed record TonApiPublicKeyResponse
{
    [JsonPropertyName("public_key")]
    public string PublicKey { get; set; }
}