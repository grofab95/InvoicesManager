using IM.BusinessLogic.Processing;
using IM.BusinessLogic.Storage;
using IM.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace IM.BusinessLogic.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddBusinessLogic(this IServiceCollection services)
    {
        return services
            .AddScoped<IAttachmentProcessor, AttachmentProcessor>()
            .AddScoped<IPathProvider, InvoicePathProvider>()
            .AddScoped<IManager, Manager>();
    }
}