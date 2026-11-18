import { createApi } from "@reduxjs/toolkit/query/react";
import { baseQueryWithReauth } from "./baseQueryWithReauth";

const tagTypes = ["Me", "Lobbies"] as const;

export type ApiTag = (typeof tagTypes)[number];

export const api = createApi({
  reducerPath: "api",
  baseQuery: baseQueryWithReauth,
  tagTypes,
  endpoints: () => ({}),
});
