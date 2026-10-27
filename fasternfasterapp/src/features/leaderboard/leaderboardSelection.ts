import type { LeaderboardPage, LeaderboardSortKey } from "./leaderboardApi";

export const SORTABLE_COLUMNS: {
  criteria: LeaderboardSortKey;
  label: string;
}[] = [
  { criteria: "Wins", label: "Wins" },
  { criteria: "BestWpm", label: "Best WPM" },
  { criteria: "AvgWpm", label: "Avg WPM" },
  { criteria: "AvgAccuracy", label: "Avg Accuracy" },
  { criteria: "WordsTyped", label: "Words Typed" },
  { criteria: "RacesTyped", label: "Races Typed" },
];

const DEFAULT_SELECTION: LeaderboardPage = {
  sort: "BestWpm",
  descending: true,
  page: 1,
};

function parseSort(value: string | null): LeaderboardSortKey {
  const column = SORTABLE_COLUMNS.find((col) => col.criteria === value);
  return column?.criteria ?? DEFAULT_SELECTION.sort;
}

function parseDescending(value: string | null): boolean {
  if (value === "true") return true;
  if (value === "false") return false;
  return DEFAULT_SELECTION.descending;
}

function parsePage(value: string | null): number {
  const page = Number(value);
  return Number.isInteger(page) && page >= 1 ? page : DEFAULT_SELECTION.page;
}

export function parseSelection(params: URLSearchParams): LeaderboardPage {
  return {
    sort: parseSort(params.get("sort")),
    descending: parseDescending(params.get("desc")),
    page: parsePage(params.get("page")),
  };
}

export function toSearchParams({
  sort,
  descending,
  page,
}: LeaderboardPage): URLSearchParams {
  return new URLSearchParams({
    sort,
    desc: String(descending),
    page: String(page),
  });
}
