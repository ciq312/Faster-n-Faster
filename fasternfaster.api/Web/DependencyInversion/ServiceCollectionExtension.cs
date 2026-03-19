using System.Reflection;
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
}
