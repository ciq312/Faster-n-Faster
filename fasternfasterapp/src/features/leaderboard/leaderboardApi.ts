import type {
  GetLeaderboardResults,
  LeaderboardResultDTO,
  LeaderboardSort,
} from "../../../types";
import { api } from "../../shared/api/api";

export const PAGE_SIZE = 20;

export type LeaderboardSortKey = keyof typeof LeaderboardSort;

export type LeaderboardOrder = {
  sort: LeaderboardSortKey;
  descending: boolean;
};

export type LeaderboardPage = LeaderboardOrder & {
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
    getMyRank: builder.query<LeaderboardResultDTO, LeaderboardOrder>({
      query: ({ sort, descending }) => ({
        url: "/api/leaderboards/me",
        params: { sort, descending },
      }),
      providesTags: ["Me"],
    }),
  }),
});

export const { useGetLeaderboardQuery, useGetMyRankQuery } = leaderboardApi;
