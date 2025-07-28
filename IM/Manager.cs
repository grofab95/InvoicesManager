using IM.Core.Interfaces;
using IM.Core.Models;

namespace IM;

public class Manager
{
    private readonly ILogger<Manager> _logger;
    private readonly IEmailService _emailService;
    private readonly IStorageService _storageService;
    private readonly IAttachmentProcessor _attachmentProcessor;
    private readonly IPathProvider _pathProvider;

    public Manager(
        ILogger<Manager> logger,
        IEmailService emailService,
        IStorageService storageService,
        IAttachmentProcessor attachmentProcessor,
        IPathProvider pathProvider)
    {
        _logger = logger;
        _emailService = emailService;
        _storageService = storageService;
        _attachmentProcessor = attachmentProcessor;
        _pathProvider = pathProvider;
    }

    public async Task Process()
    {
        var emailData = await _emailService.GetNewMessages();
        
        _logger.LogInformation("Found {Count} new emails", emailData.Length);

        foreach (var (emailIndex, email) in emailData.Select((a, b) => (b, a)))
        {
            _logger.LogInformation("Processing email {Current}/{Total}", emailIndex + 1, emailData.Length);
            
            var processingResult = await ProcessEmailAttachments(email);
            
            if (processingResult.Success)
            {
                await _emailService.MarkAsProcessed(email.MessageId);
            }
        }
    }
    
    private async Task<ProcessingResult> ProcessEmailAttachments(EmailData email)
    {
        var attachmentResults = new List<bool>();
        
        foreach (var (index, attachment) in email.Attachments.Select((a, b) => (b, a)))
        { 
            var processedAttachment = _attachmentProcessor.ProcessAttachment(attachment);
            var directoryPath = _pathProvider.GetDirectoryPath();
            var fileName = GenerateFileName(email.Date, index, attachment.Name);
            
            var uploaded = await _storageService.UploadFile(
                processedAttachment.FileBytes, 
                directoryPath, 
                fileName);
                
            attachmentResults.Add(uploaded);
        }
        
        return new ProcessingResult
        {
            Success = attachmentResults.All(result => result),
            ProcessedAttachmentsCount = attachmentResults.Count
        };
    }
    
    private static string GenerateFileName(DateTime emailDate, int index, string originalName)
    {
        return $"{emailDate:yyyyMMddHHmmss}-{index + 1}-{originalName}.pdf";
    }
}