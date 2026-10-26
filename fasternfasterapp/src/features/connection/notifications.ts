import type { UnknownAction } from "@reduxjs/toolkit";
import type { BannerVariant } from "../../shared/banner/bannerSlice";
import { hubEvents } from "./hubEvents";

export type Notification = { variant: BannerVariant; message: string };

export function notificationFor(action: UnknownAction): Notification | null {
  if (hubEvents.playerJoined.match(action)) {
    return {
      variant: "message",
      message: `player ${action.payload.nick} joined the lobby`,
    };
  }
  if (hubEvents.playerKicked.match(action)) {
    return {
      variant: "message",
      message: `player ${action.payload.nick} was kicked from lobby`,
    };
  }
  if (hubEvents.playerDisconnected.match(action)) {
    return {
      variant: "message",
      message: `player ${action.payload.disconnectedUserNick} disconnected from lobby`,
    };
  }
  if (hubEvents.kicked.match(action)) {
    return { variant: "message", message: "You were kicked from lobby" };
  }
  if (hubEvents.hostChanged.match(action)) {
    return {
      variant: "message",
      message: `New host is ${action.payload.newHostNick}`,
    };
  }
  if (hubEvents.playerFinished.match(action)) {
    const { nick, finishPosition, wpm } = action.payload;
    return {
      variant: "message",
      message: `${nick} finished ${finishPosition} with wpm:${Math.trunc(Number(wpm))}`,
    };
  }
  if (hubEvents.raceWithdrawn.match(action)) {
    return {
      variant: "message",
      message: "You were removed from this race for suspicious input",
    };
  }
  if (hubEvents.anotherSessionStarted.match(action)) {
    return {
      variant: "message",
      message: "Someone else logged into your account",
    };
  }
  if (hubEvents.banned.match(action)) {
    return { variant: "error", message: action.payload || "You are banned" };
  }
  if (hubEvents.suspended.match(action)) {
    return {
      variant: "error",
      message: `You are suspended until ${new Date(action.payload.expiresAt).toLocaleString()}`,
    };
  }
  return null;
}
