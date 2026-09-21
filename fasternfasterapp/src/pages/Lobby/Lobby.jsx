import { useCallback } from "react";
import { useLocation, useParams } from "react-router-dom";
import { useAuth } from "../../features/auth/AuthContext";
import LobbyPlayerCard from "../../features/game/components/LobbyPlayerCard/LobbyPlayerCard";
import RaceResults from "../../features/game/components/RaceResults";
import TypingArea from "../../features/game/components/TypingArea/TypingArea";
import { useLobby } from "../../features/game/hooks/useLobby";
import { useLobbyActions } from "../../features/game/hooks/useLobbyActions";
import { useRace } from "../../features/game/hooks/useRace";
import { useRaceActions } from "../../features/game/hooks/useRaceActions";
import Navbar from "../../shared/components/Navbar/Navbar";
import "./Lobby.css";

function Lobby() {
  const { lobbyId } = useParams();
  const location = useLocation();
  const {isSelf, userId: selfId} = useAuth();
  
  const {
    isHost,
    players,
    lobbyName,
    lobbyMaxPlayers,
    lobbyInviteCode,
    colors,
  } = useLobby();
  const {
    changeColor,
    kickPlayer,
    transferHost,
    leaveLobby
  } = useLobbyActions();
  const {
    tier,
    dismissResults,
    isRacing,
    isRaceStarting,
    raceSettings,
    raceResults,
    raceParticipants,
    countdown,
  } = useRace();
  const {
    startRace,
    sendProgress,
    flushProgress,
    refreshPassage,
    changeGameMode,
    changeTimerDuration,
    changeWordCount,
  } = useRaceActions();
  const maxPerSide = 10;
  const half = Math.min(players.length, maxPerSide);

  const handleProgress = useCallback(
    (update) => {
      sendProgress(update);
      if (update.final) flushProgress();
    },
    [sendProgress, flushProgress],
  );

  return (
    <div className="lobby-page">
      <Navbar />
      <div className="lobby-body">
        <header className="lobby-topbar">
          <div className="lobby-topbar__info">
            <h2 className="lobby-topbar__name">{lobbyName}</h2>
            <span className="lobby-topbar__count">
              {players.length}/{lobbyMaxPlayers} players
            </span>
            {lobbyInviteCode && (
              <span className="lobby-topbar__invite-code">
                code: {lobbyInviteCode}
              </span>
            )}
            {raceSettings && (
              <span className="lobby-topbar__mode">
                {raceSettings.$type === "word"
                  ? `word race · ${raceSettings.wordCount} words`
                  : `timer · ${raceSettings.timerDuration}s`}
              </span>
            )}
          </div>
          <button className="lobby-topbar__leave" onClick={leaveLobby}>
            leave lobby
          </button>
        </header>

        <div className="lobby-arena">
          <div className="lobby-players-col">
            {players.slice(0, half).map((p) => (
              <div key={p.id} className="lobby-player-row">
                <LobbyPlayerCard
                  player={p}
                  colors={colors}
                  changeColor={changeColor}
                  isSelf={isSelf(p.id)}
                  kickPlayer={kickPlayer}
                  transferHost={transferHost}
                  isHost={isHost}
                />
                {raceParticipants.find((rp) => rp.playerId === p.id) && (
                  <div className="lobby-player-row__wpm">
                    <span className="lobby-player-row__wpm-value">
                      {Math.round(
                        raceParticipants.find((rp) => rp.playerId === p.id).wpm,
                      )}
                    </span>
                    <span className="lobby-player-row__wpm-unit">wpm</span>
                  </div>
                )}
              </div>
            ))}
          </div>

          <div className="lobby-game">
            <div className="countdown-overlay">{countdown}</div>

            {tier && raceParticipants.find((p) => isSelf(p.playerId)) && (
              <div
                key={tier.label}
                className="tier"
                style={{ "--shake": `${Math.min(tier.min / 12, 10)}px` }}
              >
                {tier.label}
              </div>
            )}

            {raceResults ? (
              <RaceResults
                results={raceResults}
                selfId={selfId}
                onDismiss={dismissResults}
              />
            ) : raceSettings ? (
              <TypingArea
                passage={raceSettings.passage}
                disabled={!isRacing}
                onProgress={handleProgress}
                opponents={raceParticipants}
                selfId={selfId}
              />
            ) : (
              <div className="lobby-game__waiting">Loading...</div>
            )}
            {!(isRaceStarting || isRacing || raceResults) && isHost && (
              <button
                className="lobby-next-passage__icon"
                onClick={refreshPassage}
              >
                &gt;
              </button>
            )}
          </div>

          <div className="lobby-players-col">
            {players.slice(half).map((p) => (
              <LobbyPlayerCard key={p.id} player={p} />
            ))}
          </div>
        </div>

        <footer className="lobby-footer">
          {isHost && (
            <button
              className="lobby-footer__start"
              disabled={isRaceStarting || isRacing}
              onClick={startRace}
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
