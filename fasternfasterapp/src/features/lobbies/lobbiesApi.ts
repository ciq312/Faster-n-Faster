import type {
  CreateLobbyRequest,
  CreateLobbyResult,
  GetLobbiesResult,
  LobbyListItem,
} from "../../../types";
import { api } from "../../shared/api/api";
import { invalidatesOnSuccess } from "../../shared/api/invalidatesOnSuccess";

export const lobbiesApi = api.injectEndpoints({
  endpoints: (builder) => ({
    getLobbies: builder.query<LobbyListItem[], void>({
      query: () => "/api/lobbies",
      transformResponse: (response: GetLobbiesResult) => response.lobbies ?? [],
      providesTags: ["Lobbies"],
    }),
    findLobbyByInviteCode: builder.query<LobbyListItem | null, string>({
      query: (inviteCode) => ({ url: "/api/lobbies", params: { inviteCode } }),
      transformResponse: (response: GetLobbiesResult) =>
        response.lobbies?.[0] ?? null,
    }),
    createLobby: builder.mutation<CreateLobbyResult, CreateLobbyRequest>({
      query: (body) => ({ url: "/api/lobbies", method: "POST", body }),
      invalidatesTags: invalidatesOnSuccess("Lobbies"),
    }),
  }),
});

export const {
  useGetLobbiesQuery,
  useLazyFindLobbyByInviteCodeQuery,
  useCreateLobbyMutation,
} = lobbiesApi;
