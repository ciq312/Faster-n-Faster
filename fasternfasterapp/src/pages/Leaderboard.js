import { act, useCallback } from "react";
import { useNavigate } from "react-router-dom";
import { useFetchLeaderboard } from "../features/leaderboard/hooks/useFetchLeaderboard";
import Navbar from "../shared/components/Navbar";
import ErrorBanner from "../shared/components/ErrorBanner";
import "./Leaderboard.css";
import { useLobbyContext } from "../features/game/hooks/LobbyProvider";

const SORTABLE_COLUMNS = [
  { criteria: "Wins", label: "Wins" },
  { criteria: "BestWPM", label: "Best WPM" },
  { criteria: "AvgWPM", label: "Avg WPM" },
  { criteria: "AvgAccuracy", label: "Avg Accuracy" },
  { criteria: "WordsTyped", label: "Words Typed" },
  { criteria: "RacesTyped", label: "Races Typed" },
];

const PLAYER_COUNTS = [5, 10, 50, 100];

function getMedalClass(index) {
  if (index === 0) return "leaderboard-table__row--gold";
  if (index === 1) return "leaderboard-table__row--silver";
  if (index === 2) return "leaderboard-table__row--bronze";
  return "";
}

function formatStat(value, suffix) {
  if (typeof value !== "number") return "—";
  const formatted = Number.isInteger(value) ? value : value.toFixed(1);
  return suffix ? `${formatted}${suffix}` : formatted;
}

function Leaderboard() {
  const { lobbyId } = useLobbyContext();
  const navigate = useNavigate();

  const {
    players,
    loading,
    error,
    setError,
    criteria,
    isDescending,
    playersCount,
    setPlayersCount,
    sortBy,
  } = useFetchLeaderboard();

  const clearError = useCallback(() => setError(null), [setError]);

  return (
    <div className="leaderboard-page">
      <Navbar />
      {lobbyId && (
        <button
          className="return-to-lobby-btn"
          onClick={() => navigate(`/lobby/${lobbyId}`)}
        >
          Back to lobby
        </button>
      )}
      <ErrorBanner message={error} onDismiss={clearError} />

      <div className="leaderboard-page__content">
        <header className="leaderboard-page__header">
          <h1 className="leaderboard-page__title">Leaderboard</h1>
          <div className="leaderboard-count">
            {PLAYER_COUNTS.map((count) => (
              <button
                key={count}
                className={`leaderboard-count__btn${playersCount === count ? " leaderboard-count__btn--active" : ""}`}
                onClick={() => setPlayersCount(count)}
              >
                {count}
              </button>
            ))}
          </div>
        </header>

        {loading && <p className="leaderboard-page__loading">Loading...</p>}

        {!loading && !error && players.length === 0 && (
          <p className="leaderboard-page__empty">
            No race data yet — be the first to race!
          </p>
        )}

        {!loading && players.length > 0 && (
          <table className="leaderboard-table">
            <thead>
              <tr>
                <th>#</th>
                <th>Player</th>
                {SORTABLE_COLUMNS.map((col) => (
                  <th
                    key={col.criteria}
                    className={`sortable${criteria === col.criteria ? " active" : ""}`}
                    onClick={() => sortBy(col.criteria)}
                  >
                    {col.label}
                    {criteria === col.criteria && (
                      <span className="leaderboard-table__sort-arrow">
                        {isDescending ? " ▼" : " ▲"}
                      </span>
                    )}
                  </th>
                ))}
              </tr>
            </thead>
            <tbody>
              {players.map((player, index) => (
                <tr key={player.id} className={getMedalClass(index)}>
                  <td>{index + 1}</td>
                  <td className="leaderboard-table__player">
                    {player.playerName}
                  </td>
                  <td>{formatStat(player.wins)}</td>
                  <td>{formatStat(player.bestWPM)}</td>
                  <td>{formatStat(player.avgWPM)}</td>
                  <td>{formatStat(player.avgAccuracy, "%")}</td>
                  <td>{formatStat(player.wordsTyped)}</td>
                  <td>{formatStat(player.racesTyped)}</td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </div>
    </div>
  );
}

export default Leaderboard;
