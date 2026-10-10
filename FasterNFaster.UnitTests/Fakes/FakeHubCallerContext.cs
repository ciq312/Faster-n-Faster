using System.Security.Claims;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.SignalR;

namespace FasterNFaster.Tests.Fakes;

public class FakeHubCallerContext(ClaimsPrincipal? user) : HubCallerContext
{
    public override string ConnectionId { get; } = Guid.NewGuid().ToString();
    public override string? UserIdentifier => null;
    public override ClaimsPrincipal? User { get; } = user;
    public override IDictionary<object, object?> Items { get; } = new Dictionary<object, object?>();
    public override IFeatureCollection Features { get; } = new FeatureCollection();
    public override CancellationToken ConnectionAborted => CancellationToken.None;

    public override void Abort() { }
}
