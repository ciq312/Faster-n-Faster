import { act } from "@testing-library/react";
import type { ReactNode } from "react";
import { Provider } from "react-redux";
import type { MeResponse } from "../../types";
import type { AppStore } from "../app/store";
import { authApi } from "../features/auth/authApi";
import { connectHub } from "../features/connection/hubThunks";
import type { FakeGameHub } from "./fakeGameHub";
import { lobbyState } from "./fixtures";
import { signedOut } from "./handlers";
import { server } from "./server";

const defaultUser: MeResponse = {
  userId: "p1",
  userName: "Player",
  role: "User",
};

export function wrapperFor(store: AppStore) {
  return function Wrapper({ children }: { children: ReactNode }) {
    return <Provider store={store}>{children}</Provider>;
  };
}

export function signIn(store: AppStore, user: MeResponse = defaultUser) {
  return store.dispatch(authApi.util.upsertQueryData("me", undefined, user));
}

export function signOut(store: AppStore) {
  server.use(...signedOut());
  return store.dispatch(authApi.endpoints.me.initiate());
}

export async function enterLobby(
  store: AppStore,
  hub: FakeGameHub,
  user?: MeResponse,
) {
  await store.dispatch(connectHub());
  await act(() => signIn(store, user));
  await act(() => hub.client().lobbyState(lobbyState()));
}
