using System.Collections.Generic;
using Newtonsoft.Json;

namespace CAServer.Verifier.Dtos;

public class VerifyFacebookUserInfoDto
{
    [JsonProperty("appId")] public string AppId { get; set; }

    [JsonProperty("type")] public string Type { get; set; }

    [JsonProperty("application")] public string Application { get; set; }

    [JsonProperty("dataAccessExpiresAt")]
    public long DataAccessExpiresAt { get; set; }

    [JsonProperty("expiresAt")] public long ExpiresAt { get; set; }

    [JsonProperty("isValid")] public bool IsValid { get; set; }

    [JsonProperty("scopes")] public List<string> Scopes { get; set; }

    [JsonProperty("userId")] public string UserId { get; set; }
}

public class FacebookUserInfoDto
{
    [JsonProperty("id")] public string Id { get; set; }

    [JsonProperty("name")] public string FirstName { get; set; }

    [JsonProperty("email")] public string Email { get; set; }

    [JsonProperty("picture")] public Dictionary<string, FacebookPictureDto> PictureDic { get; set; }

    public string GuardianType { get; set; }
    
    public string Picture { get; set; }
    
    public string LastName { get; set; }
    public string FullName { get; set; }
}

public class FacebookPictureDto
{
    [JsonProperty("height")] public int Height { get; set; }
    
    [JsonProperty("is_silhouette")] public bool IsSilhouette { get; set; }
    
    [JsonProperty("url")] public string Url { get; set; }
    
    [JsonProperty("width")] public int Width { get; set; }
    
}


public class FacebookOauthResponse
{
    [JsonProperty("access_token")] public string AccessToken { get; set; }
    
    [JsonProperty("token_type")] public string TokenType { get; set; }
    
    [JsonProperty("expires_in")] public int ExpiresIn { get; set; }
    
}


public class VerifyFacebookUserInfoResponseDto
{
    [JsonProperty("data")]
    public VerifyFacebookUserInfoResponse Data { get; set; }
    
}


public class VerifyFacebookUserInfoResponse
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