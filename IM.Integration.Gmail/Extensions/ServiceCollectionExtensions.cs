using IM.Core.Extensions;
using IM.Core.Interfaces;
using IM.Integration.Gmail.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace IM.Integration.Gmail.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddGmailIntegration(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<GmailConfiguration>(
            configuration.GetSection("Integrations:Gmail:Installed"));
        
        return services.AddInitializableService<InternalGmailService, IEmailService, IInit>();
    }
}