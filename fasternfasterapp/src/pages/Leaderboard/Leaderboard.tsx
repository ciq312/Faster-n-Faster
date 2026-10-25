import { useState } from "react";
import {
  useGetLeaderboardQuery,
  type LeaderboardPage,
  type LeaderboardSortKey,
} from "../../features/leaderboard/leaderboardApi";
import Navbar from "../../shared/components/Navbar/Navbar";
import { useQueryErrorBanner } from "../../shared/hooks/useQueryErrorBanner";
import "./Leaderboard.css";

const SORTABLE_COLUMNS: { criteria: LeaderboardSortKey; label: string }[] = [
  { criteria: "Wins", label: "Wins" },
  { criteria: "BestWpm", label: "Best WPM" },
  { criteria: "AvgWpm", label: "Avg WPM" },
  { criteria: "AvgAccuracy", label: "Avg Accuracy" },
  { criteria: "WordsTyped", label: "Words Typed" },
  { criteria: "RacesTyped", label: "Races Typed" },
];

function getMedalClass(rank?: number) {
  if (rank === 1) return "leaderboard-table__row--gold";
  if (rank === 2) return "leaderboard-table__row--silver";
  if (rank === 3) return "leaderboard-table__row--bronze";
  return "";
}

function formatStat(value: number | undefined, suffix?: string, precision = 1) {
  if (typeof value !== "number") return "—";
  const formatted = Number.isInteger(value) ? value : value.toFixed(precision);
  return suffix ? `${formatted}${suffix}` : formatted;
}

function Leaderboard() {
  const [selection, setSelection] = useState<LeaderboardPage>({
    sort: "BestWpm",
    descending: true,
    page: 1,
  });
  const { data, isLoading, isFetching, isSuccess, error } =
    useGetLeaderboardQuery(selection);
  useQueryErrorBanner(error);

  const players = data?.items ?? [];
  const totalPages = data?.totalPages ?? 1;

  const sortBy = (sort: LeaderboardSortKey) =>
    setSelection((s) => ({
      sort,
      descending: s.sort === sort ? !s.descending : true,
      page: 1,
    }));

  const goToPage = (next: number) =>
    setSelection((s) => ({
      ...s,
      page: Math.min(Math.max(1, next), Math.max(1, totalPages)),
    }));

  return (
    <div className="leaderboard-page">
      <Navbar />
      <div className="leaderboard-page__content">
        <header className="leaderboard-page__header">
          <h1 className="leaderboard-page__title">Leaderboard</h1>
          <div className="leaderboard-pager">
            <button
              className="leaderboard-pager__btn"
              onClick={() => goToPage(selection.page - 1)}
              disabled={selection.page <= 1}
              aria-label="Previous page"
            >
              ◀
            </button>
            <button
              className="leaderboard-pager__btn"
              onClick={() => goToPage(selection.page + 1)}
              disabled={selection.page >= totalPages}
              aria-label="Next page"
            >
              ▶
            </button>
          </div>
        </header>

        {isLoading && <p className="leaderboard-page__loading">Loading...</p>}

        {isSuccess && players.length === 0 && (
          <p className="leaderboard-page__empty">
            No race data yet — be the first to race!
          </p>
        )}

        {players.length > 0 && (
          <table className="leaderboard-table" aria-busy={isFetching}>
            <thead>
              <tr>
                <th>#</th>
                <th>Player</th>
                {SORTABLE_COLUMNS.map((col) => (
                  <th
                    key={col.criteria}
                    className={`sortable${selection.sort === col.criteria ? " active" : ""}`}
                    onClick={() => sortBy(col.criteria)}
                  >
                    {col.label}
                    {selection.sort === col.criteria && (
                      <span className="leaderboard-table__sort-arrow">
                        {selection.descending ? " ▼" : " ▲"}
                      </span>
                    )}
                  </th>
                ))}
              </tr>
            </thead>
            <tbody>
              {players.map((player) => (
                <tr key={player.id} className={getMedalClass(player.rank)}>
                  <td>{player.rank}</td>
                  <td className="leaderboard-table__player">
                    {player.playerName}
                  </td>
                  <td>{formatStat(player.wins)}</td>
                  <td>{formatStat(player.bestWPM)}</td>
                  <td>{formatStat(player.avgWPM)}</td>
                  <td>{formatStat(player.avgAccuracy, "%", 2)}</td>
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
