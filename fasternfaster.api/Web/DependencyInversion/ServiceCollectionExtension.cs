using System.Reflection;
using FasterNFaster.Api.Core.Interfaces;
using FasterNFaster.Api.UseCases.Helpers;

namespace FasterNFaster.Api.Web.DependencyInversion;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddHandlers(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var handlerInterface = typeof(IHandler<,>);

        var handlers = assembly.GetTypes()
            .Where(t => t is { IsAbstract: false, IsInterface: false })
            .SelectMany(t => t.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == handlerInterface)
                .Select(i => new { Interface = i, Implementation = t }));

        foreach (var handler in handlers)
            services.AddScoped(handler.Interface, handler.Implementation);

        return services;
    }

    /// <summary>
    /// Scans for interfaces extending IRepository and registers their implementations.
    /// </summary>
    public static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var markerType = typeof(IRepository);

        var repoInterfaces = assembly.GetTypes()
            .Where(t => t.IsInterface && t != markerType && markerType.IsAssignableFrom(t));

        foreach (var iface in repoInterfaces)
        {
            var implementation = assembly.GetTypes()
                .FirstOrDefault(t => t is { IsAbstract: false, IsInterface: false } && iface.IsAssignableFrom(t));

            if (implementation is not null)
                services.AddScoped(iface, implementation);
        }

        return services;
    }
}
