using System.Reflection;
using FasterNFaster.Api.Core.Interfaces.Events;
using FasterNFaster.Api.UseCases.Interfaces;

namespace FasterNFaster.Api.Web.DependencyInversion;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddHandlers(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var handlerInterfaces = new[] { typeof(IHandler<,>), typeof(IHandler<>) };

        var handlers = assembly.GetTypes()
            .Where(t => t is { IsAbstract: false, IsInterface: false })
            .SelectMany(t => t.GetInterfaces()
                .Where(i => i.IsGenericType && handlerInterfaces.Contains(i.GetGenericTypeDefinition()))
                .Select(i => new { Interface = i, Implementation = t }));

        foreach (var handler in handlers)
            services.AddScoped(handler.Interface, handler.Implementation);

        return services;
    }

    public static IServiceCollection AddDomainEventHandlers(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        var eventHandlers = assembly.GetTypes()
            .Where(t => t is { IsAbstract: false, IsInterface: false })
            .SelectMany(t => t.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IDomainEventHandler<>))
                .Select(i => new { Interface = i, Implementation = t }));

        foreach (var handler in eventHandlers)
            services.AddScoped(handler.Interface, handler.Implementation);

        return services;
    }
}
