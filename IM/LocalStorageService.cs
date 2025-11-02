using IM.Core.Interfaces;

namespace IM;

public class LocalStorageService : IStorageService
{
    private const string BaseDropboxPath = @"C:\Users\fabia\Dropbox";
    
    private readonly ILogger<LocalStorageService> _logger;

    public LocalStorageService(ILogger<LocalStorageService> logger)
    {
        _logger = logger;
    }

    public async Task<bool> UploadFile(byte[] fileBytes, string dropboxFolder, string fileName)
    {
        try
        {
            var now = DateTime.Now;

            // Build folder structure like: C:\Users\fabia\Dropbox\Faktury\2025\10.2025\Kosztowe
            // var yearFolder = now.Year.ToString();
            // var monthFolder = $"{now.Month:D2}.{yearFolder}";
            // var targetDirectory = Path.Combine(dropboxFolder, yearFolder, monthFolder, "Kosztowe");
            
            var targetDirectory = Path.Combine(BaseDropboxPath, dropboxFolder.TrimStart('\\', '/'));

            // Ensure directory exists
            Directory.CreateDirectory(targetDirectory);

            var targetPath = Path.Combine(targetDirectory, fileName);

            await File.WriteAllBytesAsync(targetPath, fileBytes);

            _logger.LogInformation("File '{FileName}' uploaded successfully to '{Path}'.", fileName, targetPath);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload file '{FileName}' to Dropbox folder '{DropboxFolder}'.", fileName, dropboxFolder);
            return false;
        }
    }
}