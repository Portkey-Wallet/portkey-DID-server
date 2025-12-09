using Newtonsoft.Json;

namespace CAServer.Verifier.Dtos;

public class GoogleUserInfoDto
{
    [JsonProperty("id")] public string Id { get; set; }
    [JsonProperty("name")] public string FullName { get; set; }
    [JsonProperty("given_name")] public string FirstName { get; set; }
    [JsonProperty("family_name")] public string LastName { get; set; }
    public string Email { get; set; }
    [JsonProperty("verified_email")] public bool VerifiedEmail { get; set; }
    public string Picture { get; set; }
}