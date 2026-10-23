import { createSelector } from "@reduxjs/toolkit";
import type { RootState } from "../../app/store";
import { authApi } from "./authApi";

export type AuthStatus = "loading" | "authenticated" | "not authenticated";

const selectMe = authApi.endpoints.me.select();

export const selectAuthStatus = (state: RootState): AuthStatus => {
  const me = selectMe(state);
  if (me.isError) return "not authenticated";
  return me.data ? "authenticated" : "loading";
};

export const selectCurrentUser = (state: RootState) =>
  selectAuthStatus(state) === "authenticated"
    ? selectMe(state).data
    : undefined;

export const selectIsGuest = (state: RootState) =>
  selectCurrentUser(state)?.role === "Guest";

export const selectIsSelf = createSelector(
  [(state: RootState) => selectCurrentUser(state)?.userId],
  (userId) => (id: string) => userId === id,
);
