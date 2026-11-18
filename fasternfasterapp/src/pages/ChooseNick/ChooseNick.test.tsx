import { screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { http, HttpResponse } from "msw";
import { describe, expect, it } from "vitest";
import { API_BASE } from "../../config";
import { signedOut } from "../../test/handlers";
import { CurrentUser, renderPage } from "../../test/renderPage";
import { server } from "../../test/server";
import ChooseNick from "./ChooseNick";

const CHANGE_NICK_URL = `${API_BASE}/api/users/me/nick`;

function signedInAs(role: string) {
  server.use(
    http.get(`${API_BASE}/api/auth/me`, () =>
      HttpResponse.json({ userId: "p1", userName: "player_1234", role }),
    ),
  );
}

function renderChooseNick() {
  renderPage(
    <>
      <CurrentUser />
      <ChooseNick />
    </>,
    { route: "/choose-nick", probes: ["/", "/lobbies"] },
  );
}

async function submitNick(nick: string) {
  const user = userEvent.setup();
  await user.type(await screen.findByPlaceholderText("enter a nickname"), nick);
  await user.click(screen.getByRole("button", { name: "continue" }));
}

describe("ChooseNick", () => {
  it("shows the default nick the player has now", async () => {
    signedInAs("Player");

    renderChooseNick();

    expect(
      await screen.findByText("you're playing as player_1234"),
    ).toBeTruthy();
  });

  it("saves the nick and opens the lobbies", async () => {
    let sentBody: unknown;
    signedInAs("Player");
    server.use(
      http.put(CHANGE_NICK_URL, async ({ request }) => {
        sentBody = await request.json();
        return HttpResponse.json({});
      }),
    );
    renderChooseNick();

    await submitNick("Speedy");

    expect(await screen.findByText("/lobbies")).toBeTruthy();
    expect(sentBody).toEqual({ nick: "Speedy" });
  });

  it("opens the lobbies without saving when skipped", async () => {
    signedInAs("Player");
    renderChooseNick();

    await userEvent
      .setup()
      .click(await screen.findByRole("link", { name: "skip for now" }));

    expect(await screen.findByText("/lobbies")).toBeTruthy();
  });

  it("shows the server reason and stays on the page when saving fails", async () => {
    signedInAs("Player");
    server.use(
      http.put(CHANGE_NICK_URL, () =>
        HttpResponse.json(
          {
            reason: "Too many requests, try again shortly",
            code: "rate_limited",
          },
          { status: 429 },
        ),
      ),
    );
    renderChooseNick();

    await submitNick("Speedy");

    expect(
      await screen.findByText("Too many requests, try again shortly"),
    ).toBeTruthy();
    expect(screen.getByRole("button", { name: "continue" })).toBeTruthy();
  });

  it("sends a guest to the start page", async () => {
    signedInAs("Guest");

    renderChooseNick();

    expect(await screen.findByText("/")).toBeTruthy();
  });

  it("sends a logged-out visitor to the start page", async () => {
    server.use(...signedOut());

    renderChooseNick();

    expect(await screen.findByText("/")).toBeTruthy();
  });
});
