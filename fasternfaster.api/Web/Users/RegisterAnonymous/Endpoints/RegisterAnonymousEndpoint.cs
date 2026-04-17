using System.Data;
using FastEndpoints;
using FasterNFaster.Api.UseCases.Interfaces;
using FasterNFaster.Api.UseCases.Users.RegisterAnonymous.Commands;
using FasterNFaster.Api.UseCases.Users.RegisterAnonymous.Results;
using FasterNFaster.Api.Web.Services;

namespace FasterNFaster.Api.Web.Users.RegisterAnonymous.Endpoints;

public class RegisterAnonymousRequest
{
    public string Nick { get; set; } = null!;
}

public class RegisterAnonymousEndpoint(IHandler<RegisterAnonymousCommand, RegisterAnonymousResult> handler, JwtTokenService tokenService) : Endpoint<RegisterAnonymousRequest, RegisterAnonymousResult>
{
    private readonly IHandler<RegisterAnonymousCommand, RegisterAnonymousResult> _handler = handler;
    private readonly JwtTokenService tokenService = tokenService;

    public override void Configure()
    {
        Post("/api/auth/guest");
        AllowAnonymous();
    }

    public override async Task HandleAsync(RegisterAnonymousRequest req, CancellationToken ct)
    {

        try
        {
            var result = await _handler.Handle(new RegisterAnonymousCommand(req.Nick));
            var token = tokenService.CreateGuestAccessToken(new TokenCreationRequest { UserId = result.UserId, UserName = result.UserName });
            tokenService.SetAccessTokenGuestCookie(HttpContext.Response, token);
            await Send.CreatedAtAsync("RegisterAnonymous", null, result);
        }
        catch (DuplicateNameException e)
        {
            ThrowError(e.Message, 409);
        }

    }
}
