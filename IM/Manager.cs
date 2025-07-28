using IM.Core.Interfaces;
using IM.Core.Models;
using IM.Tools;

namespace IM;

public class Manager
{
    private readonly ILogger<Manager> _logger;
    private readonly IEmailService _emailService;
    private readonly IStorageService _storageService;

    public Manager(ILogger<Manager> logger,
        IEmailService emailService,
        IStorageService storageService)
    {
        _logger = logger;
        _emailService = emailService;
        _storageService = storageService;
    }

    public async Task Process()
    {
        var emailData = await _emailService.GetNewMessages();
        
        _logger.LogInformation("Found {Count} new emails", emailData.Length);

        var allUploaded = true;
        foreach (var (emailIndex, email) in emailData.Select((a,b) => (b, a)))
        {
            _logger.LogInformation("Processing email {Current}/{Total}", emailIndex+1, emailData.Length);
            
            foreach (var (index, name, type, base64) in email.Attachments.Select((a, b) => (b, a.Name, a.Type, a.Base64)))
            { 
                var fileBytes = Convert.FromBase64String(base64);
                
                if (type == AttachmentType.Image)
                {
                    fileBytes = PdfCreator.CreatePdfFromJpg(fileBytes);
                }
                
                var now = DateTime.Now;
                //var year = "2026";
                var year = now.Year.ToString();
                var monthNumber = now.Month.ToString("00"); 
                var monthYear = $"{monthNumber}.{year}";
                var directoryPath = $"/Faktury/{year}/{monthYear}/Kosztowe";
                
                //var fileName = $"{now:yyyyMMddHHmmss}-{name}.pdf";
                var fileName = $"{email.Date:yyyyMMddHHmmss}-{index+1}-{name}.pdf";
                
                var uploaded = await _storageService.UploadFile(fileBytes, directoryPath, fileName);
                if (!uploaded)
                {
                    allUploaded = false;
                }
            }
            
            if (allUploaded)
            {
                await _emailService.MarkAsProcessed(email.MessageId);
            }
        }
    }
}