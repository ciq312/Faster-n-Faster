import { act, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { http, HttpResponse } from "msw";
import { useLocation } from "react-router-dom";
import { describe, expect, it } from "vitest";
import { API_BASE } from "../../config";
import { authApi } from "../../features/auth/authApi";
import { leaderboardApi } from "../../features/leaderboard/leaderboardApi";
import { deferred } from "../../test/deferred";
import { CurrentUser, renderPage } from "../../test/renderPage";
import { server } from "../../test/server";
import { signIn } from "../../test/store";
import Leaderboard from "./Leaderboard";

const player = { userId: "me", userName: "Me", role: "Player" };

function LocationKey() {
  return <output aria-label="location key">{useLocation().key}</output>;
}

function renderLeaderboard(query = "") {
  return renderPage(
    <>
      <Leaderboard />
      <LocationKey />
    </>,
    { route: `/leaderboard${query}` },
  );
}

function serveLeaderboard({
  hold,
  items,
}: {
  hold?: { selection: string; promise: Promise<void> };
  items?: object[];
} = {}) {
  const requests: URLSearchParams[] = [];
  server.use(
    http.get(`${API_BASE}/api/leaderboards`, async ({ request }) => {
      const params = new URL(request.url).searchParams;
      requests.push(params);
      const selection = `${params.get("sort")} ${params.get("descending")} p${params.get("page")}`;
      if (hold?.selection === selection) await hold.promise;
      return HttpResponse.json({
        items: items ?? [{ id: selection, rank: 1, nick: selection }],
        totalPages: 3,
      });
    }),
  );
  return requests;
}

function serveMyRank(rank = 57) {
  const requests: URLSearchParams[] = [];
  server.use(
    http.get(`${API_BASE}/api/leaderboards/me`, ({ request }) => {
      requests.push(new URL(request.url).searchParams);
      return HttpResponse.json({ id: "me", rank, nick: "Me" });
    }),
  );
  return requests;
}

function lastRequest(requests: URLSearchParams[]) {
  return requests[requests.length - 1];
}

describe("Leaderboard", () => {
  it("requests the sort, direction and page from the URL", async () => {
    serveLeaderboard();

    renderLeaderboard("?sort=Wins&desc=false&page=2");

    expect(await screen.findByText("Wins false p2")).toBeTruthy();
  });

  it("requests the default selection for invalid URL params", async () => {
    serveLeaderboard();

    renderLeaderboard("?sort=Bogus&desc=maybe&page=-4");

    expect(await screen.findByText("BestWpm true p1")).toBeTruthy();
  });

  it("requests 20 rows per page", async () => {
    const requests = serveLeaderboard();
    renderLeaderboard();
    await screen.findByText("BestWpm true p1");

    expect(lastRequest(requests)?.get("pageSize")).toBe("20");
  });

  it("jumps to a numbered page", async () => {
    serveLeaderboard();
    renderLeaderboard();
    const user = userEvent.setup();
    await screen.findByText("BestWpm true p1");

    await user.click(screen.getByRole("button", { name: "3" }));

    expect(await screen.findByText("BestWpm true p3")).toBeTruthy();
  });

  it("marks the current page in the pager", async () => {
    serveLeaderboard();
    renderLeaderboard("?page=2");
    await screen.findByText("BestWpm true p2");

    expect(
      screen.getByRole("button", { name: "2" }).getAttribute("aria-current"),
    ).toBe("page");
    expect(
      screen.getByRole("button", { name: "3" }).getAttribute("aria-current"),
    ).toBeNull();
  });

  it("moves with the arrow buttons", async () => {
    serveLeaderboard();
    renderLeaderboard("?page=2");
    const user = userEvent.setup();
    await screen.findByText("BestWpm true p2");

    await user.click(screen.getByRole("button", { name: "Next page" }));
    await screen.findByText("BestWpm true p3");
    await user.click(screen.getByRole("button", { name: "Previous page" }));

    expect(await screen.findByText("BestWpm true p2")).toBeTruthy();
  });

  it("restarts at page 1 when the sort changes", async () => {
    serveLeaderboard();
    renderLeaderboard("?page=3");
    const user = userEvent.setup();
    await screen.findByText("BestWpm true p3");

    await user.click(screen.getByText("Wins"));

    expect(await screen.findByText("Wins true p1")).toBeTruthy();
  });

  it("moves to the last page when the URL page is past the end", async () => {
    serveLeaderboard();

    renderLeaderboard("?page=9");

    expect(await screen.findByText("BestWpm true p3")).toBeTruthy();
    expect(
      screen.getByRole("button", { name: "3" }).getAttribute("aria-current"),
    ).toBe("page");
  });

  it("does not add a history entry when the current page is clicked", async () => {
    serveLeaderboard();
    renderLeaderboard();
    const user = userEvent.setup();
    await screen.findByText("BestWpm true p1");
    const key = screen.getByLabelText("location key").textContent;

    await user.click(screen.getByRole("button", { name: "1" }));

    expect(screen.getByLabelText("location key").textContent).toBe(key);
  });

  it("drops the player's row after logging in as another player", async () => {
    serveLeaderboard();
    let loggedInAsOther = false;
    server.use(
      http.get(`${API_BASE}/api/auth/me`, () => HttpResponse.json(player)),
      http.post(`${API_BASE}/api/auth/login`, () => {
        loggedInAsOther = true;
        return HttpResponse.json({});
      }),
      http.get(`${API_BASE}/api/leaderboards/me`, () =>
        loggedInAsOther
          ? HttpResponse.json(
              { reason: "Not on the leaderboard", code: "not_on_leaderboard" },
              { status: 404 },
            )
          : HttpResponse.json({ id: "me", rank: 57, nick: "Me" }),
      ),
    );
    const { store } = renderPage(
      <>
        <Leaderboard />
        <CurrentUser />
      </>,
      { route: "/leaderboard" },
    );
    await screen.findByRole("button", { name: /go to my page/ });

    await act(() =>
      store.dispatch(
        authApi.endpoints.login.initiate({ login: "other", password: "x" }),
      ),
    );

    await waitFor(() => expect(document.querySelector("tfoot")).toBeNull());
  });

  it("highlights the player's row on the current page", async () => {
    serveLeaderboard({
      items: [
        { id: "other", rank: 1, nick: "Other" },
        { id: "me", rank: 2, nick: "Me" },
      ],
    });
    serveMyRank(2);
    const { store } = renderLeaderboard();
    await act(() => signIn(store, player));

    await waitFor(() =>
      expect(screen.getByText("Me").closest("tr")?.className).toContain(
        "leaderboard-table__row--self",
      ),
    );
    expect(screen.getByText("Other").closest("tr")?.className).not.toContain(
      "leaderboard-table__row--self",
    );
    expect(document.querySelector("tfoot")).toBeNull();
  });

  it("pins the player's row below the table when it's on another page", async () => {
    serveLeaderboard();
    serveMyRank();
    const { store } = renderLeaderboard();
    await act(() => signIn(store, player));

    const pinned = await screen.findByRole("button", { name: /go to my page/ });

    expect(pinned.textContent).toBe("57");
    expect(pinned.closest("tfoot")?.textContent).toContain("Me");
  });

  it("goes to the player's page when the pinned row is clicked", async () => {
    serveLeaderboard();
    serveMyRank(57);
    const { store } = renderLeaderboard();
    await act(() => signIn(store, player));
    const user = userEvent.setup();

    await user.click(
      await screen.findByRole("button", { name: /go to my page/ }),
    );

    expect(await screen.findByText("BestWpm true p3")).toBeTruthy();
  });

  it("asks for the player's row with the current sort and direction", async () => {
    serveLeaderboard();
    const requests = serveMyRank();
    const { store } = renderLeaderboard("?sort=Wins&desc=false");
    await act(() => signIn(store, player));

    await screen.findByRole("button", { name: /go to my page/ });

    expect(lastRequest(requests)?.get("sort")).toBe("Wins");
    expect(lastRequest(requests)?.get("descending")).toBe("false");
  });

  it("does not request the player's row for a guest", async () => {
    serveLeaderboard();
    const requests = serveMyRank();
    const { store } = renderLeaderboard();
    await act(() => signIn(store, { ...player, role: "Guest" }));

    await screen.findByText("BestWpm true p1");

    expect(requests).toHaveLength(0);
  });

  it("does not request the player's row when logged out", async () => {
    serveLeaderboard();
    const requests = serveMyRank();
    renderLeaderboard();

    await screen.findByText("BestWpm true p1");

    expect(requests).toHaveLength(0);
  });

  it("shows no pinned row and no banner when the player is not on the leaderboard", async () => {
    serveLeaderboard();
    server.use(
      http.get(`${API_BASE}/api/leaderboards/me`, () =>
        HttpResponse.json(
          { reason: "Not on the leaderboard", code: "not_on_leaderboard" },
          { status: 404 },
        ),
      ),
    );
    const { store } = renderLeaderboard();
    await act(() => signIn(store, player));
    await screen.findByText("BestWpm true p1");

    await waitFor(() =>
      expect(
        leaderboardApi.endpoints.getMyRank.select({
          sort: "BestWpm",
          descending: true,
        })(store.getState()).isError,
      ).toBe(true),
    );

    expect(document.querySelector("tfoot")).toBeNull();
    expect(document.querySelector(".error-banner")).toBeNull();
  });

  it("shows the server reason when the player's row fails to load", async () => {
    serveLeaderboard();
    server.use(
      http.get(`${API_BASE}/api/leaderboards/me`, () =>
        HttpResponse.json(
          { reason: "Rank unavailable", code: "internal_error" },
          { status: 500 },
        ),
      ),
    );
    const { store } = renderLeaderboard();
    await act(() => signIn(store, player));

    expect(await screen.findByText("Rank unavailable")).toBeTruthy();
  });

  it("keeps the previous page on screen while the next one loads", async () => {
    const release = deferred();
    serveLeaderboard({
      hold: { selection: "BestWpm true p2", promise: release.promise },
    });
    renderLeaderboard();
    const user = userEvent.setup();
    await screen.findByText("BestWpm true p1");

    await user.click(screen.getByRole("button", { name: "Next page" }));

    expect(screen.getByText("BestWpm true p1")).toBeTruthy();
    release.resolve();
    expect(await screen.findByText("BestWpm true p2")).toBeTruthy();
  });

  it("ends on the data for the last selection after fast sort and page changes", async () => {
    const release = deferred();
    serveLeaderboard({
      hold: { selection: "Wins true p1", promise: release.promise },
    });
    const { store } = renderLeaderboard();
    const user = userEvent.setup();
    await screen.findByText("BestWpm true p1");

    await user.click(screen.getByText("Wins"));
    await user.click(screen.getByRole("button", { name: "Next page" }));
    await screen.findByText("Wins true p2");
    release.resolve();
    await waitFor(() =>
      expect(
        leaderboardApi.endpoints.getLeaderboard.select({
          sort: "Wins",
          descending: true,
          page: 1,
        })(store.getState()).isSuccess,
      ).toBe(true),
    );

    expect(screen.getByText("Wins true p2")).toBeTruthy();
    expect(screen.queryByText("Wins true p1")).toBeNull();
    expect(document.querySelector(".error-banner")).toBeNull();
  });

  it("shows the server reason when the leaderboard fails to load", async () => {
    server.use(
      http.get(`${API_BASE}/api/leaderboards`, () =>
        HttpResponse.json(
          { reason: "Leaderboard unavailable", code: "internal_error" },
          { status: 500 },
        ),
      ),
    );

    renderLeaderboard();

    expect(await screen.findByText("Leaderboard unavailable")).toBeTruthy();
  });

  it("hides the empty text when loading fails", async () => {
    server.use(
      http.get(`${API_BASE}/api/leaderboards`, () =>
        HttpResponse.json(
          { reason: "Leaderboard unavailable", code: "internal_error" },
          { status: 500 },
        ),
      ),
    );

    renderLeaderboard();

    await screen.findByText("Leaderboard unavailable");
    expect(screen.queryByText(/No race data yet/)).toBeNull();
  });
});
