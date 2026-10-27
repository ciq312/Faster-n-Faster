import { useEffect, useMemo } from "react";
import { useSearchParams } from "react-router-dom";
import { useAppSelector } from "../../app/hooks";
import {
  selectIsPlayer,
  selectIsSelf,
} from "../../features/auth/authSelectors";
import Pager from "../../features/leaderboard/components/Pager";
import {
  PAGE_SIZE,
  useGetLeaderboardQuery,
  useGetMyRankQuery,
  type LeaderboardSortKey,
} from "../../features/leaderboard/leaderboardApi";
import {
  parseSelection,
  SORTABLE_COLUMNS,
  toSearchParams,
} from "../../features/leaderboard/leaderboardSelection";
import { isNotOnLeaderboard } from "../../features/leaderboard/notOnLeaderboard";
import Navbar from "../../shared/components/Navbar/Navbar";
import { useQueryErrorBanner } from "../../shared/hooks/useQueryErrorBanner";
import type { LeaderboardResultDTO } from "../../../types";
import "./Leaderboard.css";

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

function PlayerCells({ player }: { player: LeaderboardResultDTO }) {
  return (
    <>
      <td className="leaderboard-table__player">{player.nick}</td>
      <td>{formatStat(player.wins)}</td>
      <td>{formatStat(player.bestWPM)}</td>
      <td>{formatStat(player.avgWPM)}</td>
      <td>{formatStat(player.avgAccuracy, "%", 2)}</td>
      <td>{formatStat(player.wordsTyped)}</td>
      <td>{formatStat(player.racesTyped)}</td>
    </>
  );
}

function Leaderboard() {
  const [params, setParams] = useSearchParams();
  const selection = useMemo(() => parseSelection(params), [params]);
  const { data, isLoading, isFetching, isSuccess, error } =
    useGetLeaderboardQuery(selection);
  useQueryErrorBanner(error);

  const isPlayer = useAppSelector(selectIsPlayer);
  const isSelf = useAppSelector(selectIsSelf);
  const { currentData: myRank, error: meError } = useGetMyRankQuery(
    { sort: selection.sort, descending: selection.descending },
    { skip: !isPlayer, refetchOnMountOrArgChange: true },
  );
  useQueryErrorBanner(isNotOnLeaderboard(meError) ? undefined : meError);
  const me = meError ? undefined : myRank;

  const players = data?.items ?? [];
  const totalPages = data?.totalPages ?? 0;
  const pinned =
    me?.rank !== undefined && !players.some((player) => isSelf(player.id ?? ""))
      ? { player: me, rank: me.rank }
      : undefined;

  useEffect(() => {
    if (totalPages > 0 && selection.page > totalPages) {
      setParams(toSearchParams({ ...selection, page: totalPages }), {
        replace: true,
      });
    }
  }, [totalPages, selection, setParams]);

  const sortBy = (sort: LeaderboardSortKey) =>
    setParams(
      toSearchParams({
        sort,
        descending: selection.sort === sort ? !selection.descending : true,
        page: 1,
      }),
    );

  const goToPage = (target: number) => {
    const page = Math.min(Math.max(1, target), Math.max(1, totalPages));
    if (page === selection.page) return;
    setParams(toSearchParams({ ...selection, page }));
  };

  return (
    <div className="leaderboard-page">
      <Navbar />
      <div className="leaderboard-page__content">
        <header className="leaderboard-page__header">
          <h1 className="leaderboard-page__title">Leaderboard</h1>
          <Pager
            current={selection.page}
            total={totalPages}
            onChange={goToPage}
          />
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
                <tr
                  key={player.id}
                  className={`${getMedalClass(player.rank)}${isSelf(player.id ?? "") ? " leaderboard-table__row--self" : ""}`}
                >
                  <td>{player.rank}</td>
                  <PlayerCells player={player} />
                </tr>
              ))}
            </tbody>
            {pinned && (
              <tfoot>
                <tr
                  className="leaderboard-table__row--self leaderboard-table__row--pinned"
                  onClick={() => goToPage(Math.ceil(pinned.rank / PAGE_SIZE))}
                >
                  <td>
                    {/* No onClick: the row handles it, so a click doesn't push history twice. */}
                    <button
                      className="leaderboard-table__rank-btn"
                      aria-label={`Rank ${pinned.rank}, go to my page`}
                    >
                      {pinned.rank}
                    </button>
                  </td>
                  <PlayerCells player={pinned.player} />
                </tr>
              </tfoot>
            )}
          </table>
        )}
      </div>
    </div>
  );
}

export default Leaderboard;
