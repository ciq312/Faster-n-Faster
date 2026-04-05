import { useParams } from "react-router-dom";
import { useLobby } from "../features/game/hooks/useLobby";
import { useRace } from "../features/game/hooks/useRace";
import Navbar from "../shared/components/Navbar";
import LobbyPlayerCard from "../features/game/components/LobbyPlayerCard";
import TypingArea from "../features/game/components/TypingArea";
import RaceResults from "../features/game/components/RaceResults";
import "./Lobby.css";

function Lobby() {
  const { lobbyId } = useParams();
  const lobby = useLobby(lobbyId);
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
            {race.raceSettings && (
              <span className="lobby-topbar__mode">
                {race.raceSettings.gameMode === "wordcount"
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
                canOpenPalette={lobby.isSelf(p.id)}
                changeColor={lobby.changeColor}
                isSelf={lobby.isSelf(p.id)}
              />
            ))}
          </div>

          <div className="lobby-game">
            <div className="countdown-overlay">{race.countdown}</div>
            {race.raceResults ? (
              <RaceResults
                results={race.raceResults}
                selfId={lobby.selfId}
                onDismiss={race.dismissResults}
              />
            ) : race.passage ? (
              <TypingArea
                passage={race.passage}
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
            {!(race.isRaceStarting || race.isRacing) && (
              <button
                className="lobby-footer__start"
                onClick={race.refreshPassage}
              >
                refresh
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
          <button
            className="lobby-footer__start"
            disabled={race.isRaceStarting || race.isRacing}
            onClick={race.startRace}
          >
            start race
          </button>
        </footer>
      </div>
    </div>
  );
}

export default Lobby;
