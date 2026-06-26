using System.Data;
using FastEndpoints;
using FasterNFaster.Api.Core.Exceptions;
using FasterNFaster.Api.UseCases.Interfaces;
using FasterNFaster.Api.UseCases.Interfaces.Auth;
using FasterNFaster.Api.UseCases.Users.RegisterAnonymous.Results;
using FasterNFaster.Api.Web.Services;
using FasterNFaster.Api.Web.Services.Interfaces;

namespace FasterNFaster.Api.Web.Users.RegisterAnonymous.Endpoints;

public class RegisterAnonymousRequest
{
    public string Nick { get; set; } = null!;
}

public class RegisterAnonymousEndpoint(ITokenService tokenService) : Endpoint<RegisterAnonymousRequest, RegisterAnonymousResult>
{
    private readonly ITokenService tokenService = tokenService;

    public override void Configure()
    {
        Post("/api/auth/guest");
        AllowAnonymous();
    }

    public override async Task HandleAsync(RegisterAnonymousRequest req, CancellationToken ct)
    {
        try
        {
            var GuestId = Guid.NewGuid();

            await tokenService.HandleGuestAuth(GuestId, req.Nick);

            await Send.CreatedAtAsync<RegisterAnonymousEndpoint>(new { GuestId = GuestId }, cancellation: ct);
        }
        catch (DuplicateNameException e)
        {
            throw new ConflictException(e.Message);
        }

    }
}
