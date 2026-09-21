using System.Security.Cryptography;
using DotNet.Testcontainers.Containers;
using FasterNFaster.Api.Infrastructure.Db;
using FasterNFaster.Api.UseCases.Interfaces.Auth;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Npgsql.PostgresTypes;
using StackExchange.Redis;
using Testcontainers.PostgreSql;
using Testcontainers.Redis;

namespace FasterNFaster.IntegrationTests;

public class TestApplicationFactory<TProgram>
    : WebApplicationFactory<TProgram>, IAsyncLifetime where TProgram : class
{
    private readonly string appSettingsFile = "appsettings.Test.json";
    private readonly PostgreSqlContainer postgres;
    private readonly RedisContainer redis;
    private readonly IConfiguration config;

    public TestApplicationFactory()
    {
        config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetParent(AppContext.BaseDirectory)!.FullName)
            .AddJsonFile(appSettingsFile)
            .Build();

        postgres = new PostgreSqlBuilder("postgres:15-alpine")
            .WithDatabase(config["Postgres_Db"])
            .WithUsername(config["Postgres_User"])
            .WithPassword(config["Postgres_Password"])
            .Build();

        redis = new RedisBuilder("redis:7-alpine")
            .Build();
    }
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        using var rsa = RSA.Create(2048);
        var jwtPrivateKey = Convert.ToBase64String(rsa.ExportRSAPrivateKey());

        builder
        .UseEnvironment("Test")
        .UseSetting("JwtOptions:JWT_PRIVATE_TOKEN", jwtPrivateKey)
        .UseSetting("Google:ClientId", "test-client-id")
        .UseSetting("Google:ClientSecret", "test-client-secret")
        .UseSetting("ConnectionStrings:DefaultConnection", postgres.GetConnectionString())
        .UseSetting("ConnectionStrings:Redis", $"{redis.GetConnectionString()},allowAdmin=true")
        .UseContentRoot(Directory.GetCurrentDirectory())
        .ConfigureAppConfiguration(cfg => cfg.AddJsonFile(appSettingsFile))
        .ConfigureTestServices(services =>
        {
            services.RemoveAll<IEmailSender>();
            services.AddScoped<IEmailSender, FakeEmailSender>();
        });

        base.ConfigureWebHost(builder);
    }

    public Task InitializeAsync()
    {
        return Task.WhenAll(postgres.StartAsync(), redis.StartAsync());
    }

    public new Task DisposeAsync()
    {
        return Task.WhenAll(postgres.StopAsync(), redis.StopAsync());
    }

    public async Task ResetAsync()
    {
        await ExecuteScopedAsync<AppDbContext>(async db =>
        {
            var tables = db.Model.GetEntityTypes()
                .Select(entity => entity.GetTableName())
                .Where(name => name is not null)
                .Distinct()
                .Select(name => $"\"{name}\"");

            await db.Database.ExecuteSqlRawAsync(
                $"TRUNCATE {string.Join(", ", tables)} RESTART IDENTITY CASCADE");
        });

        await ExecuteScopedAsync<IConnectionMultiplexer>(async multiplexer =>
        {
            foreach (var endpoint in multiplexer.GetEndPoints())
                await multiplexer.GetServer(endpoint).FlushDatabaseAsync();
        });
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
