import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import { lobbyState } from "../../../../test/fixtures";
import LobbyPlayerCard from "./LobbyPlayerCard";

const state = lobbyState();
const [host, other] = state.players;

function renderOtherPlayersCard({ isHost = true } = {}) {
  const kickPlayer = vi.fn();
  const transferHost = vi.fn();
  render(
    <LobbyPlayerCard
      player={other}
      isHost={isHost}
      kickPlayer={kickPlayer}
      transferHost={transferHost}
    />,
  );
  return { kickPlayer, transferHost };
}

function renderSelfWithPalette(colors = state.colors) {
  const changeColor = vi.fn();
  const { container } = render(
    <LobbyPlayerCard
      player={host}
      colors={colors}
      changeColor={changeColor}
      isSelf
      isHost
    />,
  );
  return { changeColor, container };
}

function swatches(container: HTMLElement) {
  return container.querySelectorAll(".player-card__choose");
}

describe("LobbyPlayerCard", () => {
  it("lets the host kick another player", async () => {
    const user = userEvent.setup();
    const { kickPlayer } = renderOtherPlayersCard();

    await user.click(screen.getByText("Other"));
    await user.click(screen.getByRole("button", { name: "kick player" }));

    expect(kickPlayer).toHaveBeenCalledWith("p2");
    expect(screen.queryByRole("button")).toBeNull();
  });

  it("lets the host promote another player", async () => {
    const user = userEvent.setup();
    const { transferHost } = renderOtherPlayersCard();

    await user.click(screen.getByText("Other"));
    await user.click(screen.getByRole("button", { name: "promote" }));

    expect(transferHost).toHaveBeenCalledWith("p2");
    expect(screen.queryByRole("button")).toBeNull();
  });

  it("closes host actions on a click outside", async () => {
    const user = userEvent.setup();
    renderOtherPlayersCard();

    await user.click(screen.getByText("Other"));
    expect(screen.getAllByRole("button")).toHaveLength(2);
    await user.click(document.body);

    expect(screen.queryByRole("button")).toBeNull();
  });

  it("hides host actions on the host's own card", async () => {
    const user = userEvent.setup();
    renderSelfWithPalette();

    await user.click(screen.getByText("Player"));

    expect(screen.queryByRole("button")).toBeNull();
  });

  it("hides host actions from a non-host", async () => {
    const user = userEvent.setup();
    renderOtherPlayersCard({ isHost: false });

    await user.click(screen.getByText("Other"));

    expect(screen.queryByRole("button")).toBeNull();
  });

  it("changes to an available colour picked from the palette", async () => {
    const user = userEvent.setup();
    const { changeColor, container } = renderSelfWithPalette();

    await user.click(container.querySelector(".player-card__color--self")!);
    await user.click(swatches(container)[0]);

    expect(changeColor).toHaveBeenCalledWith("red");
    expect(swatches(container)).toHaveLength(0);
  });

  it("ignores a taken colour", async () => {
    const user = userEvent.setup();
    const { changeColor, container } = renderSelfWithPalette([
      { color: "red", isAvailable: false },
      { color: "blue", isAvailable: true },
    ]);

    await user.click(container.querySelector(".player-card__color--self")!);
    await user.click(swatches(container)[0]);

    expect(changeColor).not.toHaveBeenCalled();
    expect(swatches(container)).toHaveLength(2);
  });

  it("closes the palette on a click outside", async () => {
    const user = userEvent.setup();
    const { container } = renderSelfWithPalette();

    await user.click(container.querySelector(".player-card__color--self")!);
    expect(swatches(container)).toHaveLength(2);
    await user.click(document.body);

    expect(swatches(container)).toHaveLength(0);
  });

  it("does not open the palette on another player's card", async () => {
    const user = userEvent.setup();
    const { container } = render(
      <LobbyPlayerCard player={other} colors={state.colors} />,
    );

    await user.click(container.querySelector(".player-card__color")!);

    expect(swatches(container)).toHaveLength(0);
  });
});
