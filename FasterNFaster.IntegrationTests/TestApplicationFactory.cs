using System.Security.Cryptography;
using FasterNFaster.Api.Infrastructure.Db;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FasterNFaster.IntegrationTests;

public class TestApplicationFactory<TProgram>
    : WebApplicationFactory<TProgram> where TProgram : class
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        using var rsa = RSA.Create(2048);
        var jwtPrivateKey = Convert.ToBase64String(rsa.ExportRSAPrivateKey());

        builder.UseSetting("JwtOptions:JWT_PRIVATE_TOKEN", jwtPrivateKey);
        builder.UseSetting("Google:ClientId", "test-client-id");
        builder.UseSetting("Google:ClientSecret", "test-client-secret");
        builder.UseSetting("ConnectionStrings:DefaultConnection", "Host=localhost;Port=5432;Database=fasternfaster_tests;Username=admin;Password=12345678;Maximum Pool Size=300;");
        builder.UseSetting("ConnectionStrings:Redis", "localhost:6379");

        base.ConfigureWebHost(builder);
    }

    public Task ResetDb()
    {
        return ExecuteScopedAsync<AppDbContext>(context => context.Database.ExecuteSqlRawAsync(""" TRUNCATE TABLE "Users" CASCADE """));
    }

    public async Task<TResult> ExecuteScopedAsync<TService, TResult>(Func<TService, Task<TResult>> action)
    where TService : notnull
    {
        using var scope = Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<TService>();
        return await action(service);
    }

    public async Task ExecuteScopedAsync<TService>(Func<TService, Task> action)
    where TService : notnull
    {
        using var scope = Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<TService>();
        await action(service);
    }

}
