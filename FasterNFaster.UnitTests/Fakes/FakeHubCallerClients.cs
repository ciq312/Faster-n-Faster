using Microsoft.AspNetCore.SignalR;

namespace FasterNFaster.Tests.Fakes;

public class FakeHubCallerClients : IHubCallerClients
{
    public List<(string ConnectionId, string Method)> Sent { get; } = [];

    public ISingleClientProxy Client(string connectionId) => new RecordingProxy(connectionId, this);

    IClientProxy IHubClients<IClientProxy>.Client(string connectionId) => Client(connectionId);

    ISingleClientProxy IHubCallerClients.Caller => throw new NotSupportedException();
    IClientProxy IHubCallerClients<IClientProxy>.Caller => throw new NotSupportedException();
    IClientProxy IHubCallerClients<IClientProxy>.Others => throw new NotSupportedException();
    IClientProxy IHubCallerClients<IClientProxy>.OthersInGroup(string groupName) => throw new NotSupportedException();

    IClientProxy IHubClients<IClientProxy>.All => throw new NotSupportedException();
    IClientProxy IHubClients<IClientProxy>.AllExcept(IReadOnlyList<string> excludedConnectionIds) => throw new NotSupportedException();
    IClientProxy IHubClients<IClientProxy>.Clients(IReadOnlyList<string> connectionIds) => throw new NotSupportedException();
    IClientProxy IHubClients<IClientProxy>.Group(string groupName) => throw new NotSupportedException();
    IClientProxy IHubClients<IClientProxy>.GroupExcept(string groupName, IReadOnlyList<string> excludedConnectionIds) => throw new NotSupportedException();
    IClientProxy IHubClients<IClientProxy>.Groups(IReadOnlyList<string> groupNames) => throw new NotSupportedException();
    IClientProxy IHubClients<IClientProxy>.User(string userId) => throw new NotSupportedException();
    IClientProxy IHubClients<IClientProxy>.Users(IReadOnlyList<string> userIds) => throw new NotSupportedException();

    private sealed class RecordingProxy(string connectionId, FakeHubCallerClients owner) : ISingleClientProxy
    {
        public Task SendCoreAsync(string method, object?[] args, CancellationToken cancellationToken = default)
        {
            owner.Sent.Add((connectionId, method));
            return Task.CompletedTask;
        }

        public Task<T> InvokeCoreAsync<T>(string method, object?[] args, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }
}
