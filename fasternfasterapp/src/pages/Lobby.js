import { useNavigate, useParams } from "react-router-dom";
import { useLobbyConnection } from "../features/game/hooks/useLobbyConnection";
import Navbar from "../shared/components/Navbar";
import LobbyPlayerCard from "../features/game/components/LobbyPlayerCard";
import TypingArea from "../features/game/components/TypingArea";
import RaceResults from "../features/game/components/RaceResults";
import "./Lobby.css";

function Lobby() {
  const { lobbyId } = useParams();
  const lobby = useLobbyConnection(lobbyId);
  const navigate = useNavigate();
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
            {lobby.raceSettings && (
              <span className="lobby-topbar__mode">
                {lobby.raceSettings.gameMode === "wordcount"
                  ? `word count · ${lobby.raceSettings.wordCount} words`
                  : `timer · ${lobby.raceSettings.timerDuration}s`}
              </span>
            )}
          </div>
          <button
            className="lobby-topbar__leave"
            onClick={() => navigate("/lobbies")}
          >
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
                openPalette={lobby.isSelf}
                changeColor={lobby.changeColor}
              />
            ))}
          </div>

          <div className="lobby-game">
            <div className="countdown-overlay">{lobby.countdown}</div>
            {lobby.raceResults ? (
              <RaceResults
                results={lobby.raceResults}
                selfId={lobby.selfId}
                onDismiss={lobby.dismissResults}
              />
            ) : lobby.passage ? (
              <TypingArea
                passage={lobby.passage}
                disabled={!lobby.isRacing}
                onProgress={lobby.sendProgress}
                opponents={lobby.opponents}
                selfId={lobby.selfId}
              />
            ) : (
              <div className="lobby-game__waiting">
                waiting for host to start the race...
              </div>
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
            disabled={lobby.isRaceStarting || lobby.isRacing}
            onClick={lobby.startRace}
          >
            start race
          </button>
        </footer>
      </div>
    </div>
  );
}

export default Lobby;
