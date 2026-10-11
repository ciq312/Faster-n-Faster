using System.Text.Json;
using FasterNFaster.Api.Domain.Exceptions;
using FasterNFaster.Api.Presentation.Web.Shared;
using FasterNFaster.Api.UseCases.Auth.Exceptions;
using Microsoft.AspNetCore.Http;

namespace FasterNFaster.UnitTests.Exceptions;

public class StatusExceptionHandlerTests
{
    private readonly StatusExceptionHandler handler = new();

    private static DefaultHttpContext CreateContext() => new() { Response = { Body = new MemoryStream() } };

    private static JsonDocument ReadBody(HttpContext context)
    {
        context.Response.Body.Position = 0;
        return JsonDocument.Parse(context.Response.Body);
    }

    [Fact]
    public async Task PlainStatusException_WritesErrorOnly()
    {
        var context = CreateContext();

        var handled = await handler.TryHandleAsync(context, new NotFoundException("x"), CancellationToken.None);

        using var body = ReadBody(context);
        Assert.True(handled);
        Assert.Equal(StatusCodes.Status404NotFound, context.Response.StatusCode);
        Assert.Single(body.RootElement.EnumerateObject());
        Assert.Equal("x", body.RootElement.GetProperty("error").GetString());
    }

    [Fact]
    public async Task NonStatusException_ReturnsFalseAndWritesNothing()
    {
        var context = CreateContext();

        var handled = await handler.TryHandleAsync(context, new InvalidOperationException(), CancellationToken.None);

        Assert.False(handled);
        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
        Assert.Equal(0, context.Response.Body.Length);
    }

    [Fact]
    public async Task EmailNotVerifiedException_WritesErrorCodeAndEmail()
    {
        var context = CreateContext();

        await handler.TryHandleAsync(context, new EmailNotVerifiedException("player@example.com"), CancellationToken.None);

        using var body = ReadBody(context);
        Assert.Equal(StatusCodes.Status403Forbidden, context.Response.StatusCode);
        Assert.Equal(3, body.RootElement.EnumerateObject().Count());
        Assert.Equal("Email verification required", body.RootElement.GetProperty("error").GetString());
        Assert.Equal(ErrorCodes.EmailNotVerified, body.RootElement.GetProperty("code").GetString());
        Assert.Equal("player@example.com", body.RootElement.GetProperty("email").GetString());
    }

    [Fact]
    public async Task CodeWithoutExtensions_WritesErrorAndCodeOnly()
    {
        var context = CreateContext();

        await handler.TryHandleAsync(context, new CustomStatusException("message", "SOME_CODE", null), CancellationToken.None);

        using var body = ReadBody(context);
        Assert.Equal(2, body.RootElement.EnumerateObject().Count());
        Assert.Equal("message", body.RootElement.GetProperty("error").GetString());
        Assert.Equal("SOME_CODE", body.RootElement.GetProperty("code").GetString());
    }

    [Fact]
    public async Task ExtensionNamedLikeReservedField_DoesNotOverwriteIt()
    {
        var context = CreateContext();
        var extensions = new Dictionary<string, object?> { ["error"] = "overwritten", ["code"] = "OVERWRITTEN" };

        await handler.TryHandleAsync(context, new CustomStatusException("message", "SOME_CODE", extensions), CancellationToken.None);

        using var body = ReadBody(context);
        Assert.Equal("message", body.RootElement.GetProperty("error").GetString());
        Assert.Equal("SOME_CODE", body.RootElement.GetProperty("code").GetString());
    }

    private class CustomStatusException(string message, string? code, IReadOnlyDictionary<string, object?>? extensions)
        : StatusException(message)
    {
        public override string? Code => code;

        public override IReadOnlyDictionary<string, object?>? Extensions => extensions;
    }
}
