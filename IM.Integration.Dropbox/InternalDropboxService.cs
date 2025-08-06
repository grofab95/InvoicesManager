using System.Text.Json;
using IM.Core.Interfaces;
using Microsoft.Extensions.Logging;
using IM.Integration.Dropbox.Configuration;
using Microsoft.Extensions.Options;

namespace IM.Integration.Dropbox;

public class InternalDropboxService : IStorageService
{
    private readonly ILogger<InternalDropboxService> _logger;
    private readonly HttpClient _httpClient;
    private readonly DropboxConfiguration _configuration;
    
    public InternalDropboxService(ILogger<InternalDropboxService> logger,
        IOptions<DropboxConfiguration> configuration,
        HttpClient httpClient)
    {
        _logger = logger;
        _httpClient = httpClient;
        _configuration = configuration.Value;
    }
    
    public async Task<bool> UploadFile(byte[] fileBytes, string dropboxFolder, string fileName)
    {
        try
        {
            if (!dropboxFolder.StartsWith("/"))
                dropboxFolder = "/" + dropboxFolder;

            if (!dropboxFolder.EndsWith("/"))
                dropboxFolder += "/";

            var dropboxPath = dropboxFolder + fileName;

            var requestUri = "https://content.dropboxapi.com/2/files/upload";

            var dropboxArgs = new
            {
                path = dropboxPath,
                mode = "add", 
                autorename = false,
                mute = false,
                strict_conflict = false
            };

            using var content = new ByteArrayContent(fileBytes);
            content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");

            var request = new HttpRequestMessage(HttpMethod.Post, requestUri)
            {
                Content = content
            };

            request.Headers.Add("Dropbox-API-Arg", JsonSerializer.Serialize(dropboxArgs));

            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var errorDetails = await response.Content.ReadAsStringAsync();
                _logger.LogError("Upload failed. Status: {Status}. Body: {Body}", response.StatusCode, errorDetails);
                return false;
            }

            _logger.LogInformation("Upload successful for {Path}", dropboxPath);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "UploadFile error");
            return false;
        }
    }
}