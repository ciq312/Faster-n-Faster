using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace FasterNFaster.IntegrationTests;

public static class WebApplicationFactoryExtensions
{
    public static async Task<TResult> ExecuteScopedAsync<TService, TResult>(
        this WebApplicationFactory<Program> app, Func<TService, Task<TResult>> action)
        where TService : notnull
    {
        using var scope = app.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<TService>();
        return await action(service);
    }

    public static async Task ExecuteScopedAsync<TService>(
        this WebApplicationFactory<Program> app, Func<TService, Task> action)
        where TService : notnull
    {
        using var scope = app.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<TService>();
        await action(service);
    }
}
