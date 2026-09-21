using FasterNFaster.Api.Infrastructure.Auth;
using FasterNFaster.Api.UseCases.Services.Users;
using Microsoft.Extensions.Options;

namespace FasterNFaster.Tests.Fakes;

public static class ConfirmTokenFactoryHelper
{
    public static readonly IOptions<ConfirmTokenOptions> DefaultOptions = Options.Create(new ConfirmTokenOptions());

    public static ConfirmTokenFactory Create() => new(DefaultOptions);
}
