using FasterNFaster.Api.UseCases.Realtime.Ports;

namespace FasterNFaster.UnitTests.Fakes;

public class FakeConnectionTerminator : IConnectionTerminator
{
    public List<Guid> Terminated { get; } = new();

    public void Terminate(Guid userId) => Terminated.Add(userId);
}
