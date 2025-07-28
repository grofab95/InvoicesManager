using IM.Core.Interfaces;
using IM.Integration.Dropbox.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace IM.Integration.Dropbox.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDropboxIntegration(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<DropboxConfiguration>(
            configuration.GetSection("Integrations:Dropbox"));

        return services.AddSingleton<IStorageService, InternalDropboxService>();
    }
}