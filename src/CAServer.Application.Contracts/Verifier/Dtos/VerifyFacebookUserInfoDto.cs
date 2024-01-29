using System.Collections.Generic;
using Newtonsoft.Json;

namespace CAServer.Verifier.Dtos;

public class VerifyFacebookUserInfoDto
{
    [JsonProperty("data")] public VerifyFacebookTokenDto Data { get; set; }

}

public class VerifyFacebookTokenDto
{
    [JsonProperty("app_id")] public string AppId { get; set; }

    [JsonProperty("type")] public string Type { get; set; }

    [JsonProperty("application")] public string Application { get; set; }

    [JsonProperty("data_access_expires_at")]
    public long DataAccessExpiresAt { get; set; }

    [JsonProperty("expires_at")] public long ExpiresAt { get; set; }

    [JsonProperty("is_valid")] public bool IsValid { get; set; }

    [JsonProperty("scopes")] public List<string> Scopes { get; set; }

    [JsonProperty("user_id")] public string UserId { get; set; }
}

public class FacebookUserInfoDto
{
    [JsonProperty("id")] public string Id { get; set; }

    [JsonProperty("name")] public string Name { get; set; }

    [JsonProperty("email")] public string Email { get; set; }

    [JsonProperty("picture")] public Dictionary<string, FacebookPictureDto> Picture { get; set; }

    public string GuardianType { get; set; }
}

public class FacebookPictureDto
{
    [JsonProperty("height")] public int Height { get; set; }
    
    [JsonProperty("is_silhouette")] public bool IsSilhouette { get; set; }
    
    [JsonProperty("url")] public string Url { get; set; }
    
    [JsonProperty("width")] public int Width { get; set; }
    
}