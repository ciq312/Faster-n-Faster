namespace FasterNFaster.Api.UseCases.AntiCheat.Ports;

public interface IStrikeRegistry
{
    int RecordRaceViolation(Guid lobbyId, Guid userId);
    void ClearRace(Guid lobbyId);
    int RecordStrike(Guid userId, DateTime now, TimeSpan window);
    void ClearStrikes(Guid userId);
}
