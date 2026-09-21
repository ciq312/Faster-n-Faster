using System.Security.Claims;
using FastEndpoints;
using FasterNFaster.Api.UseCases.Users.ExternalLogin;
using FasterNFaster.Api.Web.Options.App;
using FasterNFaster.Api.Web.Services.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace FasterNFaster.Api.Web.Users.GoogleAuth;

public class GoogleCallbackEndpoint(
    ISender sender,
    IAuthTokenWriter auth,
    IOptions<AppOptions> appOptions) : EndpointWithoutRequest
{
    private readonly ISender sender = sender;
    private readonly IAuthTokenWriter auth = auth;
    private readonly AppOptions appOptions = appOptions.Value;

    public override void Configure()
    {
        Get("/api/auth/google/callback");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
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

        var login = await sender.Send(new ExternalLoginCommand("google", subject, email, name, EmailVerified: true), ct);

        await HttpContext.SignOutAsync("External");

        auth.WriteAuth(login.Tokens);

        var returnUrl = result.Properties?.Items["returnUrl"];
        var safeReturnUrl = ResolveSafeReturnUrl(returnUrl);

        await Send.RedirectAsync(safeReturnUrl, isPermanent: false, allowRemoteRedirects: true);
    }

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
