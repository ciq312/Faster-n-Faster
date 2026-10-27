import type {
  CreateLobbyRequest,
  CreateLobbyResult,
  GetLobbiesResult,
  LobbyListItem,
} from "../../../types";
import { api } from "../../shared/api/api";
import { invalidatesOnSuccess } from "../../shared/api/invalidatesOnSuccess";

const LOBBY_PAGE_SIZE = 20;

export const lobbiesApi = api.injectEndpoints({
  endpoints: (builder) => ({
    getLobbies: builder.infiniteQuery<GetLobbiesResult, void, string | null>({
      infiniteQueryOptions: {
        initialPageParam: null,
        getNextPageParam: (lastPage) => lastPage.nextCursor ?? undefined,
        refetchCachedPages: false,
      },
      query: ({ pageParam }) => ({
        url: "/api/lobbies",
        params: { limit: LOBBY_PAGE_SIZE, cursor: pageParam ?? undefined },
      }),
      providesTags: ["Lobbies"],
    }),
    findLobbyByInviteCode: builder.query<LobbyListItem, string>({
      query: (inviteCode) =>
        `/api/lobbies/by-invite/${encodeURIComponent(inviteCode)}`,
    }),
    createLobby: builder.mutation<CreateLobbyResult, CreateLobbyRequest>({
      query: (body) => ({ url: "/api/lobbies", method: "POST", body }),
      invalidatesTags: invalidatesOnSuccess("Lobbies"),
    }),
  }),
});

export const {
  useGetLobbiesInfiniteQuery,
  useLazyFindLobbyByInviteCodeQuery,
  useCreateLobbyMutation,
} = lobbiesApi;
