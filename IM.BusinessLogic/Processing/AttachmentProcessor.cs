using IM.Core.Interfaces;
using IM.Core.Models;
using IM.Tools;
using Microsoft.Extensions.Logging;

namespace IM.BusinessLogic.Processing;

public class AttachmentProcessor : IAttachmentProcessor
{
    private readonly ILogger<AttachmentProcessor> _logger;

    public AttachmentProcessor(ILogger<AttachmentProcessor> logger)
    {
        _logger = logger;
    }
    
    public ProcessedAttachment ProcessAttachment(AttachmentData attachment)
    {
        try
        {
            var fileBytes = Convert.FromBase64String(attachment.Base64);
            
            switch (attachment.Type)
            {
                case AttachmentType.Image:
                    _logger.LogInformation("Processing image attachment: {Name}", attachment.Name);
                    return new ProcessedAttachment
                    {
                        FileBytes = PdfCreator.CreatePdfFromJpg(fileBytes)
                    };
                    
                case AttachmentType.Document:
                    _logger.LogInformation("Using PDF attachment as-is: {Name}", attachment.Name);
                    return new ProcessedAttachment
                    {
                        FileBytes = fileBytes
                    };
                    
                default:
                    _logger.LogWarning("Unsupported attachment type: {Type} for {Name}", 
                        attachment.Type, attachment.Name);
                    return new ProcessedAttachment
                    {
                        FileBytes = fileBytes
                    };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing attachment {Name}", attachment.Name);
            throw;
        }
    }
}

