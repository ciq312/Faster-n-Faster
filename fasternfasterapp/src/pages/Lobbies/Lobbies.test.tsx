import { act, screen, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { http, HttpResponse } from "msw";
import { describe, expect, it, vi } from "vitest";
import type { LobbyListItem } from "../../../types";
import {
  bannerDismissed,
  selectCurrentBanner,
} from "../../shared/banner/bannerSlice";
import { API_BASE } from "../../config";
import { deferred } from "../../test/deferred";
import {
  revealObserved,
  setObservedVisible,
} from "../../test/fakeIntersectionObserver";
import { fakeGameHub, type FakeGameHub } from "../../test/fakeGameHub";
import { renderPage } from "../../test/renderPage";
import { server } from "../../test/server";
import Lobbies from "./Lobbies";

function lobby(id: string, name: string, isPrivate = false): LobbyListItem {
  return {
    id,
    name,
    isPrivate,
    isSessionActive: false,
    playerCount: 1,
    maxPlayers: 4,
  };
}

type LobbyPage = { items: LobbyListItem[]; nextCursor: string | null };

function serveLobbies(items: LobbyListItem[]) {
  server.use(
    http.get(`${API_BASE}/api/lobbies`, () =>
      HttpResponse.json({ items, nextCursor: null }),
    ),
  );
}

function servePages(pages: Record<string, LobbyPage>) {
  const requests: URL[] = [];
  server.use(
    http.get(`${API_BASE}/api/lobbies`, ({ request }) => {
      const url = new URL(request.url);
      requests.push(url);
      return HttpResponse.json(pages[url.searchParams.get("cursor") ?? ""]);
    }),
  );
  return requests;
}

function twoPages() {
  return servePages({
    "": { items: [lobby("l1", "First room")], nextCursor: "page-2" },
    "page-2": { items: [lobby("l2", "Second room")], nextCursor: null },
  });
}

function renderLobbies() {
  const hub = fakeGameHub();
  renderPage(<Lobbies />, {
    route: "/lobbies",
    probes: ["/lobby/:lobbyId"],
    hub,
  });
  return hub;
}

function keepPageAfterCreate(hub: FakeGameHub) {
  hub.server.connectToLobby.mockRejectedValue(
    new Error("HubException: You are already in a lobby"),
  );
}

describe("Lobbies", () => {
  it("lists the lobbies from the server", async () => {
    serveLobbies([lobby("l1", "Open room")]);

    renderLobbies();

    expect(await screen.findByText("Open room")).toBeTruthy();
  });

  it("requests 20 lobbies per page", async () => {
    const requests = twoPages();

    renderLobbies();

    await screen.findByText("First room");
    expect(requests[0].searchParams.get("limit")).toBe("20");
    expect(requests[0].searchParams.has("cursor")).toBe(false);
  });

  it("loads the next page when the end of the list comes into view", async () => {
    const requests = twoPages();
    renderLobbies();
    await screen.findByText("First room");

    act(() => revealObserved());

    expect(await screen.findByText("Second room")).toBeTruthy();
    expect(screen.getByText("First room")).toBeTruthy();
    expect(requests[1].searchParams.get("cursor")).toBe("page-2");
  });

  it("shows a lobby only once when it appears on two pages", async () => {
    servePages({
      "": { items: [lobby("l1", "Shared room")], nextCursor: "page-2" },
      "page-2": {
        items: [lobby("l1", "Shared room"), lobby("l2", "Second room")],
        nextCursor: null,
      },
    });
    renderLobbies();
    await screen.findByText("Shared room");

    act(() => revealObserved());

    await screen.findByText("Second room");
    expect(screen.getAllByText("Shared room")).toHaveLength(1);
  });

  it("keeps loading while the end of the list stays in view", async () => {
    setObservedVisible(true);
    const requests = servePages({
      "": { items: [lobby("l1", "First room")], nextCursor: "page-2" },
      "page-2": { items: [lobby("l2", "Second room")], nextCursor: "page-3" },
      "page-3": { items: [lobby("l3", "Third room")], nextCursor: null },
    });

    renderLobbies();

    expect(await screen.findByText("Third room")).toBeTruthy();
    expect(requests).toHaveLength(3);
  });

  it("stops loading after the last page", async () => {
    const requests = twoPages();
    renderLobbies();
    await screen.findByText("First room");
    act(() => revealObserved());
    await screen.findByText("Second room");

    act(() => revealObserved());

    expect(requests).toHaveLength(2);
  });

  it("restarts from the first page on Refresh", async () => {
    const requests = twoPages();
    const user = userEvent.setup();
    renderLobbies();
    await screen.findByText("First room");
    act(() => revealObserved());
    await screen.findByText("Second room");

    await user.click(screen.getByRole("button", { name: "Refresh" }));

    await vi.waitFor(() => expect(requests).toHaveLength(3));
    expect(requests[2].searchParams.has("cursor")).toBe(false);
    await vi.waitFor(() =>
      expect(screen.queryByText("Second room")).toBeNull(),
    );
    expect(screen.getByText("First room")).toBeTruthy();
  });

  it("disables Refresh while the list is loading", async () => {
    const gate = deferred();
    server.use(
      http.get(`${API_BASE}/api/lobbies`, async () => {
        await gate.promise;
        return HttpResponse.json({ items: [], nextCursor: null });
      }),
    );

    renderLobbies();

    const refresh = screen.getByRole("button", { name: "Refresh" });
    expect(refresh).toHaveProperty("disabled", true);
    gate.resolve();
    await vi.waitFor(() => expect(refresh).toHaveProperty("disabled", false));
  });

  it("refetches the lobby list after creating a lobby", async () => {
    let created = false;
    let listRequests = 0;
    server.use(
      http.get(`${API_BASE}/api/lobbies`, () => {
        listRequests++;
        return HttpResponse.json({
          items: created ? [lobby("l1", "Fresh room")] : [],
          nextCursor: null,
        });
      }),
      http.post(`${API_BASE}/api/lobbies`, () => {
        created = true;
        return HttpResponse.json(
          { lobbyId: "l1", inviteCode: null },
          { status: 201 },
        );
      }),
    );
    const user = userEvent.setup();
    const hub = renderLobbies();
    keepPageAfterCreate(hub);

    await user.click(
      await screen.findByRole("button", { name: "create lobby" }),
    );
    await user.type(
      screen.getByPlaceholderText("name your lobby"),
      "Fresh room",
    );
    await user.click(screen.getByRole("button", { name: "create" }));

    expect(await screen.findByText("Fresh room")).toBeTruthy();
    expect(listRequests).toBe(2);
  });

  it("shows the server reason for an unknown invite code", async () => {
    serveLobbies([]);
    server.use(
      http.get(`${API_BASE}/api/lobbies/by-invite/:code`, () =>
        HttpResponse.json(
          { reason: "Invalid invite code.", code: "invalid_invite_code" },
          { status: 404 },
        ),
      ),
    );
    const hub = renderLobbies();

    await userEvent
      .setup()
      .type(screen.getByPlaceholderText("enter code"), "NOPE{enter}");

    expect(await screen.findByText("Invalid invite code.")).toBeTruthy();
    expect(hub.server.connectToLobby).not.toHaveBeenCalled();
  });

  it("shows the server reason and keeps the modal open when creating fails", async () => {
    let listRequests = 0;
    server.use(
      http.get(`${API_BASE}/api/lobbies`, () => {
        listRequests++;
        return HttpResponse.json({ items: [], nextCursor: null });
      }),
      http.post(`${API_BASE}/api/lobbies`, () =>
        HttpResponse.json(
          { reason: "Too many lobbies", code: "rate_limited" },
          { status: 429 },
        ),
      ),
    );
    const user = userEvent.setup();
    const hub = renderLobbies();

    await user.click(
      await screen.findByRole("button", { name: "create lobby" }),
    );
    await user.type(screen.getByPlaceholderText("name your lobby"), "Room");
    await user.click(screen.getByRole("button", { name: "create" }));

    expect(await screen.findByText("Too many lobbies")).toBeTruthy();
    expect(screen.getByPlaceholderText("name your lobby")).toBeTruthy();
    expect(hub.server.connectToLobby).not.toHaveBeenCalled();
    expect(listRequests).toBe(1);
  });

  it("shows the server reason when the invite lookup fails", async () => {
    serveLobbies([]);
    server.use(
      http.get(`${API_BASE}/api/lobbies/by-invite/:code`, () =>
        HttpResponse.json(
          { reason: "Slow down", code: "rate_limited" },
          { status: 429 },
        ),
      ),
    );
    const hub = renderLobbies();

    await userEvent
      .setup()
      .type(screen.getByPlaceholderText("enter code"), "CODE{enter}");

    expect(await screen.findByText("Slow down")).toBeTruthy();
    expect(hub.server.connectToLobby).not.toHaveBeenCalled();
  });

  it("shows the server reason when a previously found invite code is no longer valid", async () => {
    serveLobbies([]);
    let lookups = 0;
    server.use(
      http.get(`${API_BASE}/api/lobbies/by-invite/:code`, () =>
        ++lookups === 1
          ? HttpResponse.json(lobby("l1", "Private room", true))
          : HttpResponse.json(
              { reason: "Invalid invite code.", code: "invalid_invite_code" },
              { status: 404 },
            ),
      ),
    );
    const hub = fakeGameHub();
    keepPageAfterCreate(hub);
    const { store } = renderPage(<Lobbies />, { route: "/lobbies", hub });
    const user = userEvent.setup();
    const input = screen.getByPlaceholderText("enter code");
    await user.type(input, "CODE{enter}");
    await vi.waitFor(() =>
      expect(hub.server.connectToLobby).toHaveBeenCalled(),
    );
    hub.server.connectToLobby.mockClear();
    const failedJoin = selectCurrentBanner(store.getState());
    store.dispatch(bannerDismissed(failedJoin?.id ?? ""));

    await user.type(input, "{enter}");

    expect(await screen.findByText("Invalid invite code.")).toBeTruthy();
    expect(hub.server.connectToLobby).not.toHaveBeenCalled();
  });

  it("joins the lobby found by invite code", async () => {
    serveLobbies([]);
    server.use(
      http.get(`${API_BASE}/api/lobbies/by-invite/:code`, () =>
        HttpResponse.json(lobby("l1", "Private room", true)),
      ),
    );
    const hub = renderLobbies();

    await userEvent
      .setup()
      .type(screen.getByPlaceholderText("enter code"), "CODE{enter}");

    expect(await screen.findByText("/lobby/l1")).toBeTruthy();
    expect(hub.server.connectToLobby).toHaveBeenCalledWith("l1", "CODE");
  });

  it("joins a listed lobby without an invite code", async () => {
    serveLobbies([lobby("l1", "Open room")]);
    const hub = renderLobbies();

    const list = await screen.findByRole("region", { name: "Active lobbies" });
    await screen.findByText("Open room");

    await userEvent
      .setup()
      .click(within(list).getByRole("button", { name: "join lobby" }));

    expect(await screen.findByText("/lobby/l1")).toBeTruthy();
    expect(hub.server.connectToLobby).toHaveBeenCalledWith("l1", null);
  });
});
