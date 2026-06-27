using System.Security.Claims;
using FastEndpoints;
using FasterNFaster.Api.Core.Entities;
using FasterNFaster.Api.UseCases.Interfaces.Users;
using FasterNFaster.Api.UseCases.Interfaces.Auth;
using FasterNFaster.Api.Web.Options.App;
using FasterNFaster.Api.Web.Services;
using FasterNFaster.Api.Web.Services.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace FasterNFaster.Api.Web.Users.GoogleAuth;

public class GoogleCallbackEndpoint(
    IExternalLoginStore externalLoginService,
    ITokenService tokenService,
    IUserRepository userRepo,
    ISessionService sessions,
    IOptions<AppOptions> appOptions) : EndpointWithoutRequest
{
    private readonly IUserRepository userRepo = userRepo;
    private readonly IExternalLoginStore externalLoginService = externalLoginService;
    private readonly ITokenService tokenService = tokenService;
    private readonly ISessionService sessions = sessions;
    private readonly AppOptions appOptions = appOptions.Value;

    public override void Configure()
    {
        Get("/api/auth/google/callback");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        // Reads claims from the transient "External" cookie that Google's handler signed into.
        var result = await HttpContext.AuthenticateAsync("External");
        if (!result.Succeeded || result.Principal is null)
        {
            await RedirectWithErrorAsync("external_auth_failed");
            return;
        }

        var subject = result.Principal.FindFirstValue(ClaimTypes.NameIdentifier);
        var email = result.Principal.FindFirstValue(ClaimTypes.Email) ?? throw new InvalidOperationException("No email provided");
        var name = result.Principal.FindFirstValue(ClaimTypes.Name) ?? throw new InvalidOperationException("No name provided");

        if (string.IsNullOrWhiteSpace(subject))
        {
            await RedirectWithErrorAsync("missing_subject");
            return;
        }

        var user = await userRepo.GetByEmailAsync(email);

        if (user == null)
        {
            user = new User(name, null, null);
            user.SetEmail(email);
            user.SetEmailVerified();

            await userRepo.AddAsync(user);

            await externalLoginService.CreateAsyncLoginInfo(user.Id, provider: "google", subject, email);
        }

        await HttpContext.SignOutAsync("External");

        await tokenService.HandlePlayerAuth(user.Id, user.Nick);

        var returnUrl = result.Properties?.Items["returnUrl"];
        var safeReturnUrl = ResolveSafeReturnUrl(returnUrl);

        await Send.RedirectAsync(safeReturnUrl, isPermanent: false, allowRemoteRedirects: true);
    }

    // Only allow redirecting back to the configured frontend origin. Anything else
    // (different host, different scheme, malformed) falls back to the frontend root.
    private string ResolveSafeReturnUrl(string? returnUrl)
    {
        var frontendBase = appOptions.FrontendUrl ?? string.Empty;

        if (string.IsNullOrWhiteSpace(returnUrl)) return frontendBase;
        if (!Uri.TryCreate(returnUrl, UriKind.Absolute, out var requested)) return frontendBase;
        if (!Uri.TryCreate(frontendBase, UriKind.Absolute, out var allowed)) return frontendBase;

        var sameOrigin =
            string.Equals(requested.Scheme, allowed.Scheme, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(requested.Host, allowed.Host, StringComparison.OrdinalIgnoreCase) &&
            requested.Port == allowed.Port;

        return sameOrigin ? returnUrl! : frontendBase;
    }

    private async Task RedirectWithErrorAsync(string errorCode)
    {
        var frontendBase = appOptions.FrontendUrl ?? "/";
        var separator = frontendBase.Contains('?') ? "&" : "?";
        var target = $"{frontendBase}{separator}error={Uri.EscapeDataString(errorCode)}";
        await Send.RedirectAsync(target, isPermanent: false, allowRemoteRedirects: true);
    }
}
