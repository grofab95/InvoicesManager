using System.Net.Http.Json;
using System.Text.Json;
using IM.Integration.Dropbox.Configuration;
using IM.Integration.Dropbox.Extensions;
using IM.Integration.Dropbox.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace IM.Integration.Dropbox;

public class DropboxTokenValidator : IDropboxTokenValidator
{
    private const string TokenFile = @"C:\fgCode\IM\Dropbox\token.json";
    
    private readonly ILogger<DropboxTokenValidator> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly DropboxConfiguration _configuration;
    
    private TokenData _tokenData = new();

    public DropboxTokenValidator(ILogger<DropboxTokenValidator> logger,
        IHttpClientFactory httpClientFactory,
        IOptions<DropboxConfiguration> dropboxConfiguration)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _configuration = dropboxConfiguration.Value;
    }
    
    public async Task<string> EnsureValidToken(CancellationToken cancellationToken)
    {
        if (!_tokenData.IsExpired())
        {
            return _tokenData.AccessToken;
        }
        
        _tokenData = await RefreshToken(_tokenData.RefreshToken, cancellationToken);
        await SaveTokenToFile(_tokenData, cancellationToken);

        return _tokenData.AccessToken;
    }

    private async Task<TokenData> LoadTokenFromFile()
    {
        var json = await File.ReadAllTextAsync(TokenFile);
        return JsonSerializer.Deserialize<TokenData>(json)!;
    }

    private async Task SaveTokenToFile(TokenData token, CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(token, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(TokenFile, json, cancellationToken);
    }

    private async Task<TokenData> RefreshToken(string refreshToken, CancellationToken cancellationToken)
    {
        try
        {
            using var httpClient = _httpClientFactory.CreateClient();
            var request = new HttpRequestMessage(HttpMethod.Post, "https://api.dropboxapi.com/oauth2/token")
            {
                Content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["grant_type"] = "refresh_token",
                    ["refresh_token"] = refreshToken,
                    ["client_id"] = _configuration.AppKey,
                    ["client_secret"] = _configuration.AppSecret
                })
            };
            
            var response = await httpClient.SendAsync(request, cancellationToken);
            var tokenData = await response.Content.ReadFromJsonAsync<TokenData>(cancellationToken) 
                   ?? throw new Exception("Failed to refresh token");

            tokenData.ExpiresAt = DateTime.UtcNow.AddSeconds(tokenData.ExpiresIn);
            
            _logger.LogInformation("Dropbox token refreshed successfully. New expiry: {Expiry}", tokenData.ExpiresAt);
            
            return tokenData;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to refresh Dropbox token");
            return _tokenData;
        }
    }

    public async Task Init(CancellationToken cancellationToken)
    {
        if (!File.Exists(TokenFile))
        {
            await GetTokens(cancellationToken);
        }

        try
        {
            _tokenData = await LoadTokenFromFile();
            if (_tokenData.IsExpired())
            {
                _tokenData = await RefreshToken(_tokenData.RefreshToken, cancellationToken);
                await SaveTokenToFile(_tokenData, cancellationToken);
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error loading or refreshing Dropbox token");
        }
    }

    private async Task GetTokens(CancellationToken cancellationToken)
    {
        try
        {
            var httpClient = new HttpClient();
            httpClient.BaseAddress = new Uri("https://api.dropboxapi.com/");

            var request = new HttpRequestMessage(HttpMethod.Post, "oauth2/token")
            {
                Content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["code"] = _configuration.AuthCode,
                    ["grant_type"] = "authorization_code",
                    ["client_id"] = _configuration.AppKey,
                    ["client_secret"] = _configuration.AppSecret
                })
            };

            var response = await httpClient.SendAsync(request, cancellationToken);

            var tokenData = await response.Content.ReadFromJsonAsync<TokenData>(cancellationToken)
                ?? throw new Exception("Failed to retrieve Dropbox tokens");
            
            tokenData.ExpiresAt = DateTime.UtcNow.AddSeconds(tokenData.ExpiresIn);
            
            await SaveTokenToFile(tokenData, cancellationToken);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error getting Dropbox tokens");
        }
    }
}