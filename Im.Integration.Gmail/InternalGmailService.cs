using System.Reflection;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Gmail.v1;
using Google.Apis.Gmail.v1.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using IM.Core.Interfaces;
using IM.Core.Models;
using Im.Integration.Gmail.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Im.Integration.Gmail;

public class InternalGmailService : IEmailService
{
    private const string ProcessedLabelName = "PROCESSED"; 
    private const string UnknownLabelName = "UNKNOWN"; 
    
    private readonly ILogger _logger;
    private readonly GmailService _gmailService;
    private Label _processedLabel;
    private Label _unknownLabel;
    private readonly GmailConfiguration _configuration;

    public InternalGmailService(ILogger<InternalGmailService> logger,
        IOptions<GmailConfiguration> configuration)
    {
        _logger = logger;
        _configuration = configuration.Value;

        var basePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
        var tokenPath = Path.Combine(basePath, "token.json");

        var clientSecrets = new ClientSecrets
        {
            ClientId = _configuration.Client_Id,
            ClientSecret = _configuration.Client_Secret
        };

        var credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
            clientSecrets,
            [GmailService.Scope.GmailModify],
            "user",
            CancellationToken.None,
            new FileDataStore(tokenPath, true)
        ).Result;

        _gmailService = new GmailService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = "Invoices Manager"
        });
    }
    
    public async Task Init()
    {
        _processedLabel = await GetOrCreateLabelAsync(ProcessedLabelName);
        _unknownLabel = await GetOrCreateLabelAsync(UnknownLabelName);
    }

    public async Task<EmailData[]> GetNewMessages()
    {
        try
        {
            var results = new List<EmailData>();

            // 1. List messages that don't have the processed label
            var listRequest = _gmailService.Users.Messages.List("me");
            // Exclude messages with the processed label
            listRequest.Q = $"-label:{_processedLabel.Name} -label:{_unknownLabel.Name}";
            listRequest.MaxResults = 50; // Adjust as needed
            var listResponse = await listRequest.ExecuteAsync();

            if (listResponse.Messages == null || !listResponse.Messages.Any())
                return results.ToArray();

            // 2. Process each unprocessed message
            foreach (var msg in listResponse.Messages)
            {
                var message = await _gmailService.Users.Messages.Get("me", msg.Id).ExecuteAsync();
                var date = DateTimeOffset.FromUnixTimeMilliseconds(message.InternalDate ?? -1).DateTime;
                if (message.Payload?.Parts == null)
                    continue;

                // Find all image/jpeg attachments recursively
                var imageMessageParts = FindAllAttachmentParts(message.Payload.Parts, "image/jpeg");
                var documentMessageParts = FindAllAttachmentParts(message.Payload.Parts, "application/pdf");

                var attachmentsParts = imageMessageParts.Concat(documentMessageParts).ToArray();
                var attachments = new List<AttachmentData>();

                foreach (var part in attachmentsParts)
                {
                    if (part.Body?.AttachmentId == null)
                        continue;

                    var attachment = await _gmailService.Users.Messages.Attachments
                        .Get("me", msg.Id, part.Body.AttachmentId)
                        .ExecuteAsync();

                    if (string.IsNullOrEmpty(attachment.Data))
                    {
                        continue;
                    }
                    // Fix the base64 string by replacing URL-safe characters and adding padding if needed
                    var normalizedBase64 = NormalizeBase64String(attachment.Data);
                    var type = part.MimeType == "application/pdf" 
                        ? AttachmentType.Document
                        : AttachmentType.Image;
                        
                    attachments.Add(new AttachmentData(
                        Path.GetFileNameWithoutExtension(part.Filename),
                        type,
                        normalizedBase64));
                }

                if (attachments.Count > 0)
                {
                    results.Add(new EmailData(message.Id, date, attachments.ToArray()));
                }
                else
                {
                    await MarkMessage(message.Id, _unknownLabel);
                }
            }

            return results.ToArray();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error getting new messages");

            return [];
        }
    }

    public async Task MarkAsProcessed(string emailMessageId)
    {
        await MarkMessage(emailMessageId, _processedLabel);
    }
    
    private async Task MarkMessage(string emailMessageId, Label label)
    {
        try
        {
            var modifyMessageRequest = new ModifyMessageRequest
            {
                AddLabelIds = new List<string> { label.Id },
                RemoveLabelIds = new List<string> { "INBOX" }
            };
            
            await _gmailService.Users.Messages.Modify(modifyMessageRequest, "me", emailMessageId).ExecuteAsync();
            
            _logger.LogInformation("Email {EmailMessageId} marked with label {LabelName}",
                emailMessageId, label.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking email {EmailMessageId} with label {LabelName}",
                emailMessageId, label.Name);
        }
    }
    
    /// <summary>
    /// Gets an existing label by name or creates a new one if it doesn't exist
    /// </summary>
    private async Task<Label> GetOrCreateLabelAsync(string labelName)
    {
        try
        {
            // First try to get all labels
            var labels = await _gmailService.Users.Labels.List("me").ExecuteAsync();
            var existingLabel = labels.Labels.FirstOrDefault(l => l.Name == labelName);
            
            // If label exists, return it
            if (existingLabel != null)
            {
                return existingLabel;
            }
            
            // Otherwise create a new label
            var newLabel = new Label
            {
                Name = labelName,
                LabelListVisibility = "labelShow",
                MessageListVisibility = "show"
            };
            
            return await _gmailService.Users.Labels.Create(newLabel, "me").ExecuteAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting or creating label: {labelName}");
            throw;
        }
    }

    // Recursively find all parts with the given mimeType
    private List<MessagePart> FindAllAttachmentParts(IList<MessagePart> parts, string mimeType)
    {
        var found = new List<MessagePart>();
        foreach (var part in parts)
        {
            if (part.Parts != null && part.Parts.Any())
            {
                found.AddRange(FindAllAttachmentParts(part.Parts, mimeType));
            }

            if (part.MimeType == mimeType && part.Body?.AttachmentId != null)
            {
                found.Add(part);
            }
        }
        return found;
    }
    
    /// <summary>
    /// Normalizes a base64 string that might be using URL-safe encoding or missing padding
    /// </summary>
    private string NormalizeBase64String(string base64)
    {
        try
        {
            // Replace URL-safe characters with standard Base64 characters
            base64 = base64.Replace('-', '+').Replace('_', '/');
            
            // Add padding if needed
            switch (base64.Length % 4)
            {
                case 2: base64 += "=="; break;
                case 3: base64 += "="; break;
            }
            
            // Verify the string can be decoded
            Convert.FromBase64String(base64);
            
            return base64;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error normalizing base64 string");
            throw;
        }
    }
}