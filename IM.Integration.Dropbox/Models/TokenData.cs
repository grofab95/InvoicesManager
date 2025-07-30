using System.Text.Json.Serialization;

namespace IM.Integration.Dropbox.Models;

public class TokenData
{
    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; }

    [JsonPropertyName("token_type")]
    public string TokenType { get; set; }

    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; set; }

    [JsonPropertyName("refresh_token")]
    public string RefreshToken { get; set; }
    
    public DateTime ExpiresAt { get; set; }
}