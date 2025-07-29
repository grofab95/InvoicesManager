using IM.Core.Models;

namespace IM.Core.Interfaces;

public interface IEmailService : IInit
{
    Task<EmailData[]> GetNewMessages();
    Task MarkAsProcessed(EmailData email);
}