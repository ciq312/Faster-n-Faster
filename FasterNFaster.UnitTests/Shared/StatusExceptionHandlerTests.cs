using System.Text.Json;
using FasterNFaster.Api.Domain.Exceptions;
using FasterNFaster.Api.Domain.Exceptions.Lobbies;
using FasterNFaster.Api.Presentation.Web.Shared;
using FasterNFaster.Api.UseCases.Auth.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using Shouldly;

namespace FasterNFaster.UnitTests.Shared;

public class StatusExceptionHandlerTests
{
    private readonly Mock<ILogger<StatusExceptionHandler>> logger = new();
    private readonly StatusExceptionHandler handler;

    public StatusExceptionHandlerTests() => handler = new StatusExceptionHandler(logger.Object);

    private static DefaultHttpContext CreateContext() => new() { Response = { Body = new MemoryStream() } };

    private static ApiErrorResponse? ReadBody(HttpContext context)
    {
        context.Response.Body.Position = 0;
        return JsonSerializer.Deserialize<ApiErrorResponse>(context.Response.Body, JsonSerializerOptions.Web);
    }

    private void VerifyErrorLogged(Times times) =>
        logger.Verify(
            l => l.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            times);

    [Fact]
    public async Task PlainStatusException_WritesReasonAndFallbackCode()
    {
        var context = CreateContext();

        var handled = await handler.TryHandleAsync(context, new NotFoundException("x"), CancellationToken.None);

        var body = ReadBody(context);
        handled.ShouldBeTrue();
        context.Response.StatusCode.ShouldBe(StatusCodes.Status404NotFound);
        body.ShouldNotBeNull();
        body.Reason.ShouldBe("x");
        body.Code.ShouldBe("error");
        body.Errors.ShouldBeNull();
    }

    [Fact]
    public async Task EmailNotVerifiedException_WritesCodeAndEmailInErrors()
    {
        var context = CreateContext();

        await handler.TryHandleAsync(context, new EmailNotVerifiedException("player@example.com"), CancellationToken.None);

        var body = ReadBody(context);
        context.Response.StatusCode.ShouldBe(StatusCodes.Status403Forbidden);
        body.ShouldNotBeNull();
        body.Reason.ShouldBe("Email verification required");
        body.Code.ShouldBe(ErrorCodes.EmailNotVerified);
        body.Errors.ShouldNotBeNull();
        body.Errors["email"].ShouldBe(["player@example.com"]);
    }

    [Fact]
    public async Task CodeWithoutExtensions_WritesNoErrors()
    {
        var context = CreateContext();

        await handler.TryHandleAsync(context, new CustomStatusException("message", "some_code", null), CancellationToken.None);

        var body = ReadBody(context);
        body.ShouldNotBeNull();
        body.Reason.ShouldBe("message");
        body.Code.ShouldBe("some_code");
        body.Errors.ShouldBeNull();
    }

    [Fact]
    public async Task NonStatusException_Writes500WithFallbackReason()
    {
        var context = CreateContext();

        var handled = await handler.TryHandleAsync(context, new InvalidOperationException("internal details"), CancellationToken.None);

        var body = ReadBody(context);
        handled.ShouldBeTrue();
        context.Response.StatusCode.ShouldBe(StatusCodes.Status500InternalServerError);
        body.ShouldNotBeNull();
        body.Reason.ShouldBe("An unexpected error occurred");
        body.Code.ShouldBe("error");
        body.Errors.ShouldBeNull();
    }

    [Fact]
    public async Task NonStatusException_IsLoggedAsError()
    {
        await handler.TryHandleAsync(CreateContext(), new InvalidOperationException(), CancellationToken.None);

        VerifyErrorLogged(Times.Once());
    }

    [Fact]
    public async Task StatusException_IsNotLogged()
    {
        await handler.TryHandleAsync(CreateContext(), new NotFoundException("x"), CancellationToken.None);

        VerifyErrorLogged(Times.Never());
    }

    public static TheoryData<StatusException, int> MappedExceptions => new()
    {
        { new BadRequestException("x"), StatusCodes.Status400BadRequest },
        { new UnauthorizedException("x"), StatusCodes.Status401Unauthorized },
        { new ForbiddenException("x"), StatusCodes.Status403Forbidden },
        { new NotFoundException("x"), StatusCodes.Status404NotFound },
        { new ConflictException("x"), StatusCodes.Status409Conflict },
        { new LobbyFullException(), StatusCodes.Status409Conflict },
        { new InvalidCredentialsException(), StatusCodes.Status401Unauthorized },
        { new NotHostException("kick"), StatusCodes.Status403Forbidden },
        { new InvalidInviteCodeException(), StatusCodes.Status404NotFound },
        { new HostKickSelfException(), StatusCodes.Status400BadRequest },
    };

    [Theory]
    [MemberData(nameof(MappedExceptions))]
    public async Task MappedException_ReturnsItsStatusCode(StatusException exception, int expectedStatusCode)
    {
        var context = CreateContext();

        await handler.TryHandleAsync(context, exception, CancellationToken.None);

        context.Response.StatusCode.ShouldBe(expectedStatusCode);
    }

    [Fact]
    public async Task UnmappedStatusException_Returns500()
    {
        var context = CreateContext();

        await handler.TryHandleAsync(context, new CustomStatusException("message", null, null), CancellationToken.None);

        context.Response.StatusCode.ShouldBe(StatusCodes.Status500InternalServerError);
    }

    private class CustomStatusException(string message, string? code, IReadOnlyDictionary<string, string[]>? extensions)
        : StatusException(message)
    {
        public override string? Code => code;

        public override IReadOnlyDictionary<string, string[]>? Extensions => extensions;
    }
}
