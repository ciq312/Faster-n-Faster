import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import type { RaceResultDTO } from "../../../../generated/signalr/FasterNFaster.Api.UseCases.Realtime.RaceFinished";
import { raceResult } from "../../../test/fixtures";
import RaceResults from "./RaceResults";

function nicks() {
  return screen
    .getAllByRole("row")
    .slice(1)
    .map((row) => row.querySelectorAll("td")[1].textContent);
}

describe("RaceResults", () => {
  it("lists players by finish position with unfinished players last", () => {
    const results = [
      raceResult({ playerId: "p3", nick: "Third", finishPosition: undefined }),
      raceResult({ playerId: "p2", nick: "Second", finishPosition: 2 }),
      raceResult({ playerId: "p1", nick: "First", finishPosition: 1 }),
    ];

    render(<RaceResults results={results} selfId="p1" onDismiss={vi.fn()} />);

    expect(nicks()).toEqual(["First", "Second", "Third"]);
  });

  it("renders frozen results", () => {
    const results = Object.freeze([
      raceResult({ playerId: "p2", nick: "Second", finishPosition: 2 }),
      raceResult({ playerId: "p1", nick: "First", finishPosition: 1 }),
    ]) as RaceResultDTO[];

    render(<RaceResults results={results} selfId="p1" onDismiss={vi.fn()} />);

    expect(nicks()).toEqual(["First", "Second"]);
  });

  it("highlights the current player", () => {
    const results = [
      raceResult({ playerId: "p1", nick: "Me" }),
      raceResult({ playerId: "p2", nick: "Other", finishPosition: 2 }),
    ];

    render(<RaceResults results={results} selfId="p1" onDismiss={vi.fn()} />);

    expect(screen.getByText("Me").closest("tr")?.className).toBe(
      "current-player",
    );
    expect(screen.getByText("Other").closest("tr")?.className).toBe("");
  });

  it("calls onDismiss when closed", async () => {
    const onDismiss = vi.fn();
    render(
      <RaceResults
        results={[raceResult()]}
        selfId="p1"
        onDismiss={onDismiss}
      />,
    );

    await userEvent
      .setup()
      .click(screen.getByRole("button", { name: "close" }));

    expect(onDismiss).toHaveBeenCalledTimes(1);
  });
});
