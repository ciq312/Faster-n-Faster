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
  refreshPassage,
  startRace,
  transferHost,
} from "../../features/connection/hubThunks";
import Countdown from "../../features/game/components/Countdown";
import LobbyPlayerCard from "../../features/game/components/LobbyPlayerCard/LobbyPlayerCard";
import RaceResults from "../../features/game/components/RaceResults";
import TypingArea from "../../features/game/components/TypingArea/TypingArea";
import type { TypingProgress } from "../../features/game/hooks/useTyping";
import {
  progressTyped,
  resultsDismissed,
  selectCanType,
  selectCountdownSeconds,
  selectRaceParticipants,
  selectRacePhase,
  selectRaceResults,
} from "../../features/game/raceSlice";
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

const MAX_PLAYERS_PER_SIDE = 10;

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
  const phase = useAppSelector(selectRacePhase);
  const countdownSeconds = useAppSelector(selectCountdownSeconds);
  const results = useAppSelector(selectRaceResults);
  const participants = useAppSelector(selectRaceParticipants);
  const canType = useAppSelector(selectCanType);
  const settings = joinedLobby?.settings;
  const half = Math.min(players.length, MAX_PLAYERS_PER_SIDE);

  useEffect(() => {
    if (wasKicked) navigate(ROUTES.LOBBIES);
  }, [wasKicked, navigate]);

  const handleLeave = async () => {
    const result = await dispatch(leaveLobby());
    if (leaveLobby.fulfilled.match(result)) navigate(ROUTES.LOBBIES);
  };

  const onChangeColor = useCallback(
    (color: string) => dispatch(changeColor(color)),
    [dispatch],
  );
  const onKick = useCallback(
    (targetId: string) => dispatch(kickPlayer(targetId)),
    [dispatch],
  );
  const onTransferHost = useCallback(
    (targetId: string) => dispatch(transferHost(targetId)),
    [dispatch],
  );

  const onProgress = useCallback(
    (progress: TypingProgress) => dispatch(progressTyped(progress)),
    [dispatch],
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
            {settings && (
              <span className="lobby-topbar__mode">
                word race · {settings.wordCount} words
              </span>
            )}
          </div>
          <button className="lobby-topbar__leave" onClick={handleLeave}>
            leave lobby
          </button>
        </header>

        <div className="lobby-arena">
          <div className="lobby-players-col">
            {players.slice(0, half).map((p) => {
              const wpm = participants.find((rp) => rp.playerId === p.id)?.wpm;
              return (
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
                  {wpm !== undefined && (
                    <div className="lobby-player-row__wpm">
                      <span className="lobby-player-row__wpm-value">
                        {Math.round(wpm)}
                      </span>
                      <span className="lobby-player-row__wpm-unit">wpm</span>
                    </div>
                  )}
                </div>
              );
            })}
          </div>

          <div className="lobby-game">
            <div className="countdown-overlay">
              {countdownSeconds !== null && (
                <Countdown seconds={countdownSeconds} />
              )}
            </div>

            {results ? (
              <RaceResults
                results={results}
                selfId={selfId}
                onDismiss={() => dispatch(resultsDismissed())}
              />
            ) : settings ? (
              <TypingArea
                passage={settings.passage}
                disabled={!canType}
                onProgress={onProgress}
                opponents={participants}
                selfId={selfId}
              />
            ) : (
              <div className="lobby-game__waiting">Loading...</div>
            )}
            {phase === "idle" && isHost && (
              <button
                className="lobby-next-passage__icon"
                onClick={() => dispatch(refreshPassage())}
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
              disabled={phase === "countdown" || phase === "racing"}
              onClick={() => dispatch(startRace())}
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
