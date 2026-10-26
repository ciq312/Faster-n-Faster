import { screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { http, HttpResponse } from "msw";
import { describe, expect, it } from "vitest";
import { API_BASE } from "../../config";
import { leaderboardApi } from "../../features/leaderboard/leaderboardApi";
import { deferred } from "../../test/deferred";
import { renderPage } from "../../test/renderPage";
import { server } from "../../test/server";
import Leaderboard from "./Leaderboard";

function renderLeaderboard() {
  return renderPage(<Leaderboard />, { route: "/leaderboard" });
}

function serveLeaderboard(hold?: {
  selection: string;
  promise: Promise<void>;
}) {
  server.use(
    http.get(`${API_BASE}/api/leaderboards`, async ({ request }) => {
      const params = new URL(request.url).searchParams;
      const selection = `${params.get("sort")} ${params.get("descending")} p${params.get("page")}`;
      if (hold?.selection === selection) await hold.promise;
      return HttpResponse.json({
        items: [{ id: selection, rank: 1, playerName: selection }],
        totalPages: 3,
      });
    }),
  );
}

describe("Leaderboard", () => {
  it("keeps the previous page on screen while the next one loads", async () => {
    const release = deferred();
    serveLeaderboard({
      selection: "BestWpm true p2",
      promise: release.promise,
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
    serveLeaderboard({ selection: "Wins true p1", promise: release.promise });
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
