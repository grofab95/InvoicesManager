using IM.Core.Models;

namespace IM.Core.Interfaces;

public interface IAttachmentProcessor
{
    ProcessedAttachment ProcessAttachment(AttachmentData attachment);
}

public class ProcessedAttachment
{
    public byte[] FileBytes { get; set; }
}

