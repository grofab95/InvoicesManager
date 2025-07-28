using Dropbox.Api;
using Dropbox.Api.Files;
using IM.Core.Interfaces;
using Microsoft.Extensions.Logging;
using IM.Integration.Dropbox.Configuration;
using Microsoft.Extensions.Options;

namespace IM.Integration.Dropbox;

public class InternalDropboxService : IStorageService
{
    private readonly ILogger<InternalDropboxService> _logger;
    private readonly DropboxClient _client;
    
    public InternalDropboxService(ILogger<InternalDropboxService> logger,
        IOptions<DropboxConfiguration> configuration)
    {
        _logger = logger;
        _client = new DropboxClient(configuration.Value.AccessToken);
    }
    
    public async Task<bool> UploadFile(byte[] fileBytes, string dropboxFolder, string fileName)
    {
        try
        {
            if (!dropboxFolder.StartsWith("/"))
                dropboxFolder = "/" + dropboxFolder;

            if (!dropboxFolder.EndsWith("/"))
                dropboxFolder += "/";

            string dropboxPath = dropboxFolder + fileName;

            using var memStream = new MemoryStream(fileBytes);

            var result = await _client.Files.UploadAsync(
                dropboxPath,
                WriteMode.Overwrite.Instance,
                body: memStream);

            _logger.LogInformation($"Uploaded: {result.PathDisplay}");
            return true;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "UploadFile error");
            return false;
        }
    }
}