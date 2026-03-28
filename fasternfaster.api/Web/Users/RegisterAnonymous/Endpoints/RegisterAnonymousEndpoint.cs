using FastEndpoints;
using FasterNFaster.Api.UseCases.Interfaces;
using FasterNFaster.Api.UseCases.Users.RegisterAnonymous.Commands;
using FasterNFaster.Api.UseCases.Users.RegisterAnonymous.Results;

namespace FasterNFaster.Api.Web.Users.RegisterAnonymous.Endpoints;

public class RegisterAnonymousRequest
{
    public string Nick { get; set; } = null!;
}

public class RegisterAnonymousEndpoint : Endpoint<RegisterAnonymousRequest, RegisterAnonymousResult>
{
    private readonly IHandler<RegisterAnonymousCommand, RegisterAnonymousResult> _handler;

    public RegisterAnonymousEndpoint(IHandler<RegisterAnonymousCommand, RegisterAnonymousResult> handler)
    {
        _handler = handler;
    }

    public override void Configure()
    {
        Post("/api/auth/guest");
        AllowAnonymous();
    }

    public override async Task HandleAsync(RegisterAnonymousRequest req, CancellationToken ct)
    {
        var command = new RegisterAnonymousCommand(req.Nick);
        var result = await _handler.Handle(command);
        await Send.CreatedAtAsync("RegisterAnonymous", null, result);
    }
}
