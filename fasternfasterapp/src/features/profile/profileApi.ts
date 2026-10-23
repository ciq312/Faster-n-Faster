import type { GetUserProfileResult } from "../../../types";
import { api } from "../../shared/api/api";

export const profileApi = api.injectEndpoints({
  endpoints: (builder) => ({
    getMyProfile: builder.query<GetUserProfileResult, void>({
      query: () => "/api/users/me/profile",
      providesTags: ["Me"],
    }),
  }),
});

export const { useGetMyProfileQuery } = profileApi;
