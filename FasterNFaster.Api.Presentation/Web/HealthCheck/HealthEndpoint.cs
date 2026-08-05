using FastEndpoints;

namespace FasterNFaster.Api.HealthCheck;

public class HealthEndpoint : EndpointWithoutRequest<HealthResponse>
{
    public override void Configure()
    {
        Get("/api/health");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        await Send.OkAsync(new HealthResponse { Status = "healthy" }, ct);
    }
}

public class HealthResponse
{
    public string Status { get; set; } = null!;
}
