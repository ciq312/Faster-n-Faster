import { screen, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { http, HttpResponse } from "msw";
import { describe, expect, it } from "vitest";
import { API_BASE } from "../../config";
import { fakeGameHub, type FakeGameHub } from "../../test/fakeGameHub";
import { renderPage } from "../../test/renderPage";
import { server } from "../../test/server";
import Lobbies from "./Lobbies";

function lobby(id: string, name: string, isPrivate = false) {
  return {
    id,
    name,
    isPrivate,
    isSessionActive: false,
    playerCount: 1,
    maxPlayers: 4,
  };
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
  it("lists the public lobbies from the server", async () => {
    server.use(
      http.get(`${API_BASE}/api/lobbies`, () =>
        HttpResponse.json({
          lobbies: [lobby("l1", "Open room"), lobby("l2", "Secret room", true)],
        }),
      ),
    );

    renderLobbies();

    expect(await screen.findByText("Open room")).toBeTruthy();
    expect(screen.queryByText("Secret room")).toBeNull();
  });

  it("refetches the lobby list after creating a lobby", async () => {
    let created = false;
    let listRequests = 0;
    server.use(
      http.get(`${API_BASE}/api/lobbies`, () => {
        listRequests++;
        return HttpResponse.json({
          lobbies: created ? [lobby("l1", "Fresh room")] : [],
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

  it("shows Invalid invite code for an unknown code", async () => {
    server.use(
      http.get(`${API_BASE}/api/lobbies`, () =>
        HttpResponse.json({ lobbies: [] }),
      ),
    );
    const hub = renderLobbies();

    await userEvent
      .setup()
      .type(screen.getByPlaceholderText("enter code"), "NOPE{enter}");

    expect(await screen.findByText("Invalid invite code")).toBeTruthy();
    expect(hub.server.connectToLobby).not.toHaveBeenCalled();
  });

  it("shows the server reason and keeps the modal open when creating fails", async () => {
    let listRequests = 0;
    server.use(
      http.get(`${API_BASE}/api/lobbies`, () => {
        listRequests++;
        return HttpResponse.json({ lobbies: [] });
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
    server.use(
      http.get(`${API_BASE}/api/lobbies`, ({ request }) =>
        new URL(request.url).searchParams.has("inviteCode")
          ? HttpResponse.json(
              { reason: "Slow down", code: "rate_limited" },
              { status: 429 },
            )
          : HttpResponse.json({ lobbies: [] }),
      ),
    );
    const hub = renderLobbies();

    await userEvent
      .setup()
      .type(screen.getByPlaceholderText("enter code"), "CODE{enter}");

    expect(await screen.findByText("Slow down")).toBeTruthy();
    expect(hub.server.connectToLobby).not.toHaveBeenCalled();
  });

  it("joins the lobby found by invite code", async () => {
    server.use(
      http.get(`${API_BASE}/api/lobbies`, ({ request }) =>
        HttpResponse.json({
          lobbies: new URL(request.url).searchParams.has("inviteCode")
            ? [lobby("l1", "Private room", true)]
            : [],
        }),
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
    server.use(
      http.get(`${API_BASE}/api/lobbies`, () =>
        HttpResponse.json({ lobbies: [lobby("l1", "Open room")] }),
      ),
    );
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
