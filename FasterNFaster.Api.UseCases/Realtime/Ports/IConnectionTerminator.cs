namespace FasterNFaster.Api.UseCases.Realtime.Ports;

public interface IConnectionTerminator
{
    void Terminate(Guid userId);
}
