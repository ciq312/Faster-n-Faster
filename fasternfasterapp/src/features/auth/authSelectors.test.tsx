import { renderHook } from "@testing-library/react";
import { http, HttpResponse } from "msw";
import type { ReactNode } from "react";
import { Provider } from "react-redux";
import { describe, expect, it, vi } from "vitest";
import { setupStore } from "../../app/store";
import { API_BASE } from "../../config";
import { signedOut } from "../../test/handlers";
import { server } from "../../test/server";
import { authApi, useMeQuery } from "./authApi";
import {
  selectAuthStatus,
  selectCurrentUser,
  selectIsGuest,
  selectIsSelf,
} from "./authSelectors";

const player = { userId: "p1", userName: "Player", role: "User" };

function meRespondsWith(user: typeof player) {
  server.use(
    http.get(`${API_BASE}/api/auth/me`, () => HttpResponse.json(user)),
  );
}

function wrapperFor(store: ReturnType<typeof setupStore>) {
  return function Wrapper({ children }: { children: ReactNode }) {
    return <Provider store={store}>{children}</Provider>;
  };
}

async function storeWithMe(user = player) {
  meRespondsWith(user);
  const store = setupStore();
  await store.dispatch(authApi.endpoints.me.initiate());
  return store;
}

describe("auth selectors", () => {
  it("reports loading before me resolves", () => {
    const store = setupStore();

    expect(selectAuthStatus(store.getState())).toBe("loading");
  });

  it("reports authenticated with the current user after me resolves", async () => {
    const store = await storeWithMe();

    expect(selectAuthStatus(store.getState())).toBe("authenticated");
    expect(selectCurrentUser(store.getState())).toEqual(player);
  });

  it("reports not authenticated when me fails", async () => {
    server.use(...signedOut());
    const store = setupStore();

    await store.dispatch(authApi.endpoints.me.initiate());

    expect(selectAuthStatus(store.getState())).toBe("not authenticated");
    expect(selectCurrentUser(store.getState())).toBeUndefined();
  });

  it("stays authenticated while me refetches after login", async () => {
    const store = await storeWithMe();
    let refetches = 0;
    let releaseMe!: () => void;
    const meGate = new Promise<void>((resolve) => (releaseMe = resolve));
    server.use(
      http.get(`${API_BASE}/api/auth/me`, async () => {
        refetches++;
        await meGate;
        return HttpResponse.json(player);
      }),
      http.post(`${API_BASE}/api/auth/login`, () => HttpResponse.json({})),
    );

    await store.dispatch(
      authApi.endpoints.login.initiate({ login: "player", password: "secret" }),
    );
    await vi.waitFor(() => expect(refetches).toBe(1));

    expect(selectAuthStatus(store.getState())).toBe("authenticated");
    releaseMe();
    await vi.waitFor(() =>
      expect(authApi.endpoints.me.select()(store.getState()).status).toBe(
        "fulfilled",
      ),
    );
  });

  it("recognises a guest by role", async () => {
    const store = await storeWithMe({ ...player, role: "Guest" });

    expect(selectIsGuest(store.getState())).toBe(true);
  });

  it("matches only the current user's id in isSelf", async () => {
    const store = await storeWithMe();
    const isSelf = selectIsSelf(store.getState());

    expect(isSelf("p1")).toBe(true);
    expect(isSelf("p2")).toBe(false);
  });

  it("clears the current user after a successful logout", async () => {
    const store = await storeWithMe();
    renderHook(() => useMeQuery(), { wrapper: wrapperFor(store) });
    server.use(
      http.post(`${API_BASE}/api/auth/logout`, () => {
        server.use(...signedOut());
        return HttpResponse.json({});
      }),
    );

    await store.dispatch(authApi.endpoints.logout.initiate());

    await vi.waitFor(() =>
      expect(selectAuthStatus(store.getState())).toBe("not authenticated"),
    );
    expect(selectCurrentUser(store.getState())).toBeUndefined();
  });

  it("keeps the current user when logout fails", async () => {
    let meHits = 0;
    server.use(
      http.get(`${API_BASE}/api/auth/me`, () => {
        meHits++;
        return HttpResponse.json(player);
      }),
    );
    const store = setupStore();
    await store.dispatch(authApi.endpoints.me.initiate());
    renderHook(() => useMeQuery(), { wrapper: wrapperFor(store) });
    server.use(
      http.post(`${API_BASE}/api/auth/logout`, () =>
        HttpResponse.json({}, { status: 500 }),
      ),
    );

    await store.dispatch(authApi.endpoints.logout.initiate());

    expect(selectCurrentUser(store.getState())).toEqual(player);
    expect(meHits).toBe(1);
  });
});
