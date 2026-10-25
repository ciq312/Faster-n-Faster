import type { GetLeaderboardResults, LeaderboardSort } from "../../../types";
import { api } from "../../shared/api/api";

const PAGE_SIZE = 10;

export type LeaderboardSortKey = keyof typeof LeaderboardSort;

export type LeaderboardPage = {
  sort: LeaderboardSortKey;
  descending: boolean;
  page: number;
};

export const leaderboardApi = api.injectEndpoints({
  endpoints: (builder) => ({
    getLeaderboard: builder.query<GetLeaderboardResults, LeaderboardPage>({
      query: ({ sort, descending, page }) => ({
        url: "/api/leaderboards",
        params: { sort, descending, page, pageSize: PAGE_SIZE },
      }),
    }),
  }),
});

export const { useGetLeaderboardQuery } = leaderboardApi;
