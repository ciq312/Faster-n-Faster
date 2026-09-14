using System.Security.Cryptography;
using DotNet.Testcontainers.Containers;
using FasterNFaster.Api.UseCases.Interfaces.Auth;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Npgsql;
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

    public IConfiguration Configuration => config;

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

    public new async Task DisposeAsync()
    {
        await base.DisposeAsync();
        await Task.WhenAll(postgres.StopAsync(), redis.StopAsync());
        await postgres.DisposeAsync();
        await redis.DisposeAsync();
    }

    public WebApplicationFactory<TProgram> CreateApp(Action<IWebHostBuilder>? configure = null) =>
        WithWebHostBuilder(builder => configure?.Invoke(builder));

    public async Task ResetAsync()
    {
        await TruncatePostgresAsync();
        await FlushRedisAsync();
    }

    private async Task TruncatePostgresAsync()
    {
        await using var connection = new NpgsqlConnection(postgres.GetConnectionString());
        await connection.OpenAsync();

        var tables = new List<string>();
        await using (var select = new NpgsqlCommand(
            "SELECT tablename FROM pg_tables WHERE schemaname = 'public' AND tablename <> '__EFMigrationsHistory'", connection))
        await using (var reader = await select.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
                tables.Add($"\"{reader.GetString(0)}\"");
        }

        if (tables.Count == 0) return;

        await using var truncate = new NpgsqlCommand($"TRUNCATE {string.Join(", ", tables)} RESTART IDENTITY CASCADE", connection);
        await truncate.ExecuteNonQueryAsync();
    }

    private async Task FlushRedisAsync()
    {
        using var multiplexer = await ConnectionMultiplexer.ConnectAsync($"{redis.GetConnectionString()},allowAdmin=true");
        foreach (var endpoint in multiplexer.GetEndPoints())
            await multiplexer.GetServer(endpoint).FlushDatabaseAsync();
    }

}
