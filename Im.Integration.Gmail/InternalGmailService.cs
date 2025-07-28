using Microsoft.Extensions.Logging;

namespace Im.Integration.Gmail;

public class GmailService
{
    private readonly ILogger _logger;

    public GmailService(ILogger<GmailService> logger)
    {
        _logger = logger;
    }
}