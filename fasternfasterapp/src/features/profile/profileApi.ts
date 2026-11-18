import type { ChangeNickRequest, GetUserProfileResponse } from "../../../types";
import { api } from "../../shared/api/api";
import { invalidatesOnSuccess } from "../../shared/api/invalidatesOnSuccess";

export const profileApi = api.injectEndpoints({
  endpoints: (builder) => ({
    getMyProfile: builder.query<GetUserProfileResponse, void>({
      query: () => "/api/users/me/profile",
      providesTags: ["Me"],
    }),
    changeNick: builder.mutation<void, ChangeNickRequest>({
      query: (body) => ({ url: "/api/users/me/nick", method: "PUT", body }),
      invalidatesTags: invalidatesOnSuccess("Me"),
    }),
  }),
});

export const { useGetMyProfileQuery, useChangeNickMutation } = profileApi;
