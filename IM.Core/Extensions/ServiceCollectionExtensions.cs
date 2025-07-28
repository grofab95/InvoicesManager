using IM.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace IM.Core.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers a service that implements multiple interfaces including IInit
    /// </summary>
    /// <typeparam name="TImplementation">The concrete implementation type</typeparam>
    /// <typeparam name="TService1">First service interface</typeparam>
    /// <typeparam name="TService2">Second service interface</typeparam>
    /// <param name="services">The service collection</param>
    /// <param name="lifetime">Optional service lifetime (defaults to Singleton)</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddInitializableService<TImplementation, TService1, TService2>(
        this IServiceCollection services,
        ServiceLifetime lifetime = ServiceLifetime.Singleton)
        where TImplementation : class, TService1, TService2, IInit
        where TService1 : class
        where TService2 : class
    {
        // Register the implementation
        services.Add(new ServiceDescriptor(typeof(TImplementation), typeof(TImplementation), lifetime));
        
        // Register service interfaces
        services.Add(new ServiceDescriptor(typeof(TService1), sp => sp.GetRequiredService<TImplementation>(), lifetime));
        services.Add(new ServiceDescriptor(typeof(TService2), sp => sp.GetRequiredService<TImplementation>(), lifetime));
        
        // Register as IInit for initialization
        services.Add(new ServiceDescriptor(typeof(IInit), sp => sp.GetRequiredService<TImplementation>(), lifetime));

        return services;
    }

    /// <summary>
    /// Registers a service that implements a single interface plus IInit
    /// </summary>
    /// <typeparam name="TImplementation">The concrete implementation type</typeparam>
    /// <typeparam name="TService">The service interface</typeparam>
    /// <param name="services">The service collection</param>
    /// <param name="lifetime">Optional service lifetime (defaults to Singleton)</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddInitializableService<TImplementation, TService>(
        this IServiceCollection services,
        ServiceLifetime lifetime = ServiceLifetime.Singleton)
        where TImplementation : class, TService, IInit
        where TService : class
    {
        // Register the implementation
        services.Add(new ServiceDescriptor(typeof(TImplementation), typeof(TImplementation), lifetime));
        
        // Register service interface
        services.Add(new ServiceDescriptor(typeof(TService), sp => sp.GetRequiredService<TImplementation>(), lifetime));
        
        // Register as IInit for initialization
        services.Add(new ServiceDescriptor(typeof(IInit), sp => sp.GetRequiredService<TImplementation>(), lifetime));

        return services;
    }
    
    /// <summary>
    /// Registers a service that only implements IInit
    /// </summary>
    /// <typeparam name="TImplementation">The concrete implementation type</typeparam>
    /// <param name="services">The service collection</param>
    /// <param name="lifetime">Optional service lifetime (defaults to Singleton)</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddInitializableService<TImplementation>(
        this IServiceCollection services,
        ServiceLifetime lifetime = ServiceLifetime.Singleton)
        where TImplementation : class, IInit
    {
        // Register the implementation
        services.Add(new ServiceDescriptor(typeof(TImplementation), typeof(TImplementation), lifetime));
        
        // Register as IInit for initialization
        services.Add(new ServiceDescriptor(typeof(IInit), sp => sp.GetRequiredService<TImplementation>(), lifetime));

        return services;
    }
}