import { useCallback, useEffect } from "react";
import { useNavigate } from "react-router-dom";
import { useAppDispatch, useAppSelector } from "../../app/hooks";
import {
  selectCurrentUser,
  selectIsSelf,
} from "../../features/auth/authSelectors";
import {
  changeColor,
  kickPlayer,
  leaveLobby,
  transferHost,
} from "../../features/connection/hubThunks";
import LobbyPlayerCard from "../../features/game/components/LobbyPlayerCard/LobbyPlayerCard";
import RaceResults from "../../features/game/components/RaceResults";
import TypingArea from "../../features/game/components/TypingArea/TypingArea";
import { useRace } from "../../features/game/hooks/useRace";
import { useRaceActions } from "../../features/game/hooks/useRaceActions";
import { useLobbyRefresh } from "../../features/lobbies/hooks/useLobbyRefresh";
import {
  selectIsHost,
  selectJoinedLobby,
  selectPlayers,
  selectWasKicked,
} from "../../features/lobbies/lobbySelectors";
import Navbar from "../../shared/components/Navbar/Navbar";
import { ROUTES } from "../../shared/utils/routes";
import "./Lobby.css";

function Lobby() {
  const dispatch = useAppDispatch();
  const navigate = useNavigate();
  const isSelf = useAppSelector(selectIsSelf);
  const selfId = useAppSelector(selectCurrentUser)?.userId;

  const joinedLobby = useAppSelector(selectJoinedLobby);
  const players = useAppSelector(selectPlayers);
  const isHost = useAppSelector(selectIsHost);
  const wasKicked = useAppSelector(selectWasKicked);
  useLobbyRefresh();
  const {
    dismissResults,
    isRacing,
    isRaceStarting,
    raceSettings,
    raceResults,
    raceParticipants,
    countdown,
    isWithdrawn,
  } = useRace();
  const { startRace, sendProgress, flushProgress, refreshPassage } =
    useRaceActions();
  const maxPerSide = 10;
  const half = Math.min(players.length, maxPerSide);

  useEffect(() => {
    if (wasKicked) navigate(ROUTES.LOBBIES);
  }, [wasKicked, navigate]);

  const handleLeave = async () => {
    const result = await dispatch(leaveLobby());
    if (leaveLobby.fulfilled.match(result)) navigate(ROUTES.LOBBIES);
  };

  const onChangeColor = useCallback(
    (color) => dispatch(changeColor(color)),
    [dispatch],
  );
  const onKick = useCallback(
    (targetId) => dispatch(kickPlayer(targetId)),
    [dispatch],
  );
  const onTransferHost = useCallback(
    (targetId) => dispatch(transferHost(targetId)),
    [dispatch],
  );

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
            <h2 className="lobby-topbar__name">{joinedLobby?.lobbyName}</h2>
            <span className="lobby-topbar__count">
              {players.length}/{joinedLobby?.maxPlayers} players
            </span>
            {joinedLobby?.inviteCode && (
              <span className="lobby-topbar__invite-code">
                code: {joinedLobby.inviteCode}
              </span>
            )}
            {raceSettings && (
              <span className="lobby-topbar__mode">
                word race · {raceSettings.wordCount} words
              </span>
            )}
          </div>
          <button className="lobby-topbar__leave" onClick={handleLeave}>
            leave lobby
          </button>
        </header>

        <div className="lobby-arena">
          <div className="lobby-players-col">
            {players.slice(0, half).map((p) => (
              <div key={p.id} className="lobby-player-row">
                <LobbyPlayerCard
                  player={p}
                  colors={joinedLobby?.colors}
                  changeColor={onChangeColor}
                  isSelf={isSelf(p.id)}
                  kickPlayer={onKick}
                  transferHost={onTransferHost}
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

            {raceResults ? (
              <RaceResults
                results={raceResults}
                selfId={selfId}
                onDismiss={dismissResults}
              />
            ) : raceSettings ? (
              <TypingArea
                passage={raceSettings.passage}
                disabled={!isRacing || isWithdrawn}
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
