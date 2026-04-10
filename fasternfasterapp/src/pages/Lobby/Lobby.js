import { useLocation, useParams } from "react-router-dom";
import LobbyPlayerCard from "../../features/game/components/LobbyPlayerCard/LobbyPlayerCard";
import RaceResults from "../../features/game/components/RaceResults";
import TypingArea from "../../features/game/components/TypingArea/TypingArea";
import { useLobby } from "../../features/game/hooks/useLobby";
import { useRace } from "../../features/game/hooks/useRace";
import Navbar from "../../shared/components/Navbar/Navbar";
import "./Lobby.css";

function Lobby() {
  const { lobbyId } = useParams();
  const location = useLocation();
  const lobby = useLobby(lobbyId, location.state?.inviteCode);
  const race = useRace();
  const maxPerSide = 10;
  const half = Math.min(lobby.players.length, maxPerSide);

  return (
    <div className="lobby-page">
      <Navbar />
      <div className="lobby-body">
        <header className="lobby-topbar">
          <div className="lobby-topbar__info">
            <h2 className="lobby-topbar__name">{lobby.lobbyName}</h2>
            <span className="lobby-topbar__count">
              {lobby.players.length}/{lobby.lobbyMaxPlayers} players
            </span>
            {lobby.lobbyInviteCode && (
              <span className="lobby-topbar__invite-code">
                code: {lobby.lobbyInviteCode}
              </span>
            )}
            {race.settings && (
              <span className="lobby-topbar__mode">
                {race.RaceType === "WordRace"
                  ? `word count · ${race.raceSettings.wordCount} words`
                  : `timer · ${race.raceSettings.timerDuration}s`}
              </span>
            )}
          </div>
          <button className="lobby-topbar__leave" onClick={lobby.leaveLobby}>
            leave lobby
          </button>
        </header>

        <div className="lobby-arena">
          <div className="lobby-players-col">
            {lobby.players.slice(0, half).map((p) => (
              <LobbyPlayerCard
                key={p.id}
                player={p}
                colors={lobby.colors}
                changeColor={lobby.changeColor}
                isSelf={lobby.isSelf(p.id)}
              />
            ))}
          </div>

          <div className="lobby-game">
            {race.tier &&
              race.raceParticipants.find((p) => lobby.isSelf(p.playerId)) && (
                <div
                  key={race.tier.label}
                  className="tier"
                  style={{ "--shake": `${Math.min(race.tier.min / 12, 10)}px` }}
                >
                  {race.tier.label}
                </div>
              )}
            <dev className="tier">fdsafasdf</dev>
            <div className="countdown-overlay">{race.countdown}</div>
            {race.raceResults ? (
              <RaceResults
                results={race.raceResults}
                selfId={lobby.selfId}
                onDismiss={race.dismissResults}
              />
            ) : race.raceSettings ? (
              <TypingArea
                passage={race.raceSettings.passage}
                disabled={!race.isRacing}
                onProgress={race.sendProgress}
                opponents={race.raceParticipants}
                selfId={lobby.selfId}
              />
            ) : (
              <div className="lobby-game__waiting">
                waiting for host to start the race...
              </div>
            )}
            {!(race.isRaceStarting || race.isRacing || race.raceResults) &&
              lobby.isHost && (
                <button
                  className="lobby-next-passage__icon"
                  onClick={race.refreshPassage}
                >
                  &gt;
                </button>
              )}
          </div>

          <div className="lobby-players-col">
            {lobby.players.slice(half).map((p) => (
              <LobbyPlayerCard key={p.id} player={p} />
            ))}
          </div>
        </div>

        <footer className="lobby-footer">
          {lobby.isHost && (
            <button
              className="lobby-footer__start"
              disabled={race.isRaceStarting || race.isRacing}
              onClick={race.startRace}
            >
              start race
            </button>
          )}
        </footer>
      </div>
    </div>
  );
}

export default Lobby;
