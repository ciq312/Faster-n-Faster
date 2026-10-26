import { describe, expect, it } from "vitest";
import { hubEvents } from "./hubEvents";
import { notificationFor } from "./notifications";

describe("notificationFor", () => {
  it.each([
    {
      event: "playerKicked",
      action: hubEvents.playerKicked({ userId: "p2", nick: "Other" }),
      variant: "message",
      message: "player Other was kicked from lobby",
    },
    {
      event: "playerDisconnected",
      action: hubEvents.playerDisconnected({
        disconnectedUserId: "p2",
        disconnectedUserNick: "Other",
      }),
      variant: "message",
      message: "player Other disconnected from lobby",
    },
    {
      event: "kicked",
      action: hubEvents.kicked(),
      variant: "message",
      message: "You were kicked from lobby",
    },
    {
      event: "hostChanged",
      action: hubEvents.hostChanged({ userId: "p2", newHostNick: "Other" }),
      variant: "message",
      message: "New host is Other",
    },
    {
      event: "playerFinished",
      action: hubEvents.playerFinished({
        nick: "Player",
        playerId: "p1",
        finishPosition: 2,
        wpm: 87.9,
        accuracy: 99,
      }),
      variant: "message",
      message: "Player finished 2 with wpm:87",
    },
    {
      event: "raceWithdrawn",
      action: hubEvents.raceWithdrawn(),
      variant: "message",
      message: "You were removed from this race for suspicious input",
    },
    {
      event: "anotherSessionStarted",
      action: hubEvents.anotherSessionStarted(),
      variant: "message",
      message: "Someone else logged into your account",
    },
    {
      event: "banned",
      action: hubEvents.banned("Cheating"),
      variant: "error",
      message: "Cheating",
    },
    {
      event: "suspended",
      action: hubEvents.suspended({ expiresAt: "2030-01-01T12:00:00Z" }),
      variant: "error",
      message: `You are suspended until ${new Date("2030-01-01T12:00:00Z").toLocaleString()}`,
    },
  ])("maps $event to its banner text", ({ action, variant, message }) => {
    expect(notificationFor(action)).toEqual({ variant, message });
  });

  it("falls back to You are banned without a reason", () => {
    expect(notificationFor(hubEvents.banned(""))).toEqual({
      variant: "error",
      message: "You are banned",
    });
  });

  it("gives no notification for other actions", () => {
    expect(notificationFor(hubEvents.raceStarted())).toBeNull();
    expect(notificationFor({ type: "unrelated" })).toBeNull();
  });
});
