import {
  fetchBaseQuery,
  type BaseQueryFn,
  type FetchArgs,
  type FetchBaseQueryError,
} from "@reduxjs/toolkit/query";
import { API_BASE } from "../../config";
import { refreshOnce } from "../utils/refreshOnce";

export type ReauthOptions = { reauth?: boolean };

const rawBaseQuery = fetchBaseQuery({
  baseUrl: API_BASE,
  credentials: "include",
});

export const baseQueryWithReauth: BaseQueryFn<
  string | FetchArgs,
  unknown,
  FetchBaseQueryError,
  ReauthOptions
> = async (args, api, extraOptions) => {
  const result = await rawBaseQuery(args, api, extraOptions);
  if (result.error?.status !== 401 || extraOptions?.reauth === false) {
    return result;
  }
  const refreshed = await refreshOnce().then(
    (response) => response.ok,
    () => false,
  );
  return refreshed ? rawBaseQuery(args, api, extraOptions) : result;
};
