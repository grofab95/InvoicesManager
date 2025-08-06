using IM.Core.Extensions;
using IM.Core.Interfaces;
using IM.Integration.Dropbox.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace IM.Integration.Dropbox.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDropboxIntegration(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<DropboxConfiguration>(configuration.GetSection("Integrations:Dropbox"));

        services.AddInitializableService<DropboxTokenValidator, IDropboxTokenValidator>(); 

        services.AddScoped<DropboxAuthHandler>();

        services.AddHttpClient<IStorageService, InternalDropboxService>(httpClient =>
        {
            httpClient.DefaultRequestHeaders.Accept.Add(new("application/json"));
        }).AddHttpMessageHandler<DropboxAuthHandler>();
        
        return services;
    }
}