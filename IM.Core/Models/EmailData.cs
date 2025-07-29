namespace IM.Core.Models;

public enum AttachmentType
{
    Unknown,
    Image,
    Document
}

public record AttachmentData(string Name, AttachmentType Type, string Base64);

public record EmailData(string MessageId, string Subject, DateTime Date, AttachmentData[] Attachments);