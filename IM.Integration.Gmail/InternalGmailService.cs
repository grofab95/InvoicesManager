using Google.Apis.Auth.OAuth2;
using Google.Apis.Gmail.v1;
using Google.Apis.Gmail.v1.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using IM.Core.Interfaces;
using IM.Core.Models;
using IM.Integration.Gmail.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace IM.Integration.Gmail;

public class InternalGmailService : IEmailService
{
    private const string ProcessedLabelName = "PROCESSED"; 
    private const string UnknownLabelName = "UNKNOWN"; 
    
    private readonly ILogger<InternalGmailService> _logger;
    private readonly GmailService _gmailService;
    private Label _processedLabel;
    private Label _unknownLabel;

    public InternalGmailService(ILogger<InternalGmailService> logger,
        IOptions<GmailConfiguration> configuration)
    {
        _logger = logger;

        var tokenPath = Path.Combine(@"C:\fgCode\IM", "token.json");

        var clientSecrets = new ClientSecrets
        {
            ClientId = configuration.Value.Client_Id,
            ClientSecret = configuration.Value.Client_Secret
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

            var listRequest = _gmailService.Users.Messages.List("me");
            listRequest.Q = $"-label:{_processedLabel.Name} -label:{_unknownLabel.Name}";
            listRequest.MaxResults = 50;
            var listResponse = await listRequest.ExecuteAsync();

            if (listResponse.Messages == null || !listResponse.Messages.Any())
            {
                return results.ToArray();
            }

            foreach (var msg in listResponse.Messages)
            {
                var message = await _gmailService.Users.Messages.Get("me", msg.Id).ExecuteAsync();
                var subject = message.Payload?.Headers?.FirstOrDefault(h => h.Name == "Subject")?.Value ?? string.Empty;
                var date = DateTimeOffset.FromUnixTimeMilliseconds(message.InternalDate ?? -1).DateTime;
                if (message.Payload?.Parts == null)
                    continue;

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
                    var normalizedBase64 = NormalizeBase64String(attachment.Data);
                    var type = part.MimeType == "application/pdf" 
                        ? AttachmentType.Document
                        : AttachmentType.Image;
                        
                    attachments.Add(new AttachmentData(
                        Path.GetFileNameWithoutExtension(part.Filename),
                        type,
                        normalizedBase64));
                }

                var emailData = new EmailData(message.Id, subject, date, attachments.ToArray());
                if (attachments.Count > 0)
                {
                    results.Add(emailData);
                }
                else
                {
                    await MarkMessage(emailData, _unknownLabel);
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

    public async Task MarkAsProcessed(EmailData email)
    {
        await MarkMessage(email, _processedLabel);
    }
    
    private async Task MarkMessage(EmailData email, Label label)
    {
        try
        {
            var modifyMessageRequest = new ModifyMessageRequest
            {
                AddLabelIds = new List<string> { label.Id },
                RemoveLabelIds = new List<string> { "INBOX" }
            };
            
            await _gmailService.Users.Messages.Modify(modifyMessageRequest, "me", email.MessageId).ExecuteAsync();

            _logger.LogInformation("Email '{Subject}' ({EmailMessageId}) marked with label {LabelName}",
                email.Subject, email.MessageId, label.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking email '{Subject}' ({EmailMessageId}) with label {LabelName}",
                email.Subject, email.MessageId, label.Name);
        }
    }

    private async Task<Label> GetOrCreateLabelAsync(string labelName)
    {
        try
        {
            var labels = await _gmailService.Users.Labels.List("me").ExecuteAsync();
            var existingLabel = labels.Labels.FirstOrDefault(l => l.Name == labelName);

            if (existingLabel != null)
            {
                return existingLabel;
            }

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
            _logger.LogError(ex, "Error getting or creating label: {LabelName}", labelName);
            throw;
        }
    }

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

    private string NormalizeBase64String(string base64)
    {
        try
        {
            base64 = base64.Replace('-', '+').Replace('_', '/');

            switch (base64.Length % 4)
            {
                case 2: base64 += "=="; break;
                case 3: base64 += "="; break;
            }
            _ = Convert.FromBase64String(base64);
            
            return base64;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error normalizing base64 string");
            throw;
        }
    }
}