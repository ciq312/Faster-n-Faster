import { describe, expect, it } from "vitest";
import { isNotOnLeaderboard } from "./notOnLeaderboard";

const error = (status: number, code: string) => ({
  status,
  data: { reason: "x", code },
});

describe("isNotOnLeaderboard", () => {
  it("recognises the not_on_leaderboard 404", () => {
    expect(isNotOnLeaderboard(error(404, "not_on_leaderboard"))).toBe(true);
  });

  it("ignores other 404 codes", () => {
    expect(isNotOnLeaderboard(error(404, "not_found"))).toBe(false);
  });

  it("ignores non-404 errors", () => {
    expect(isNotOnLeaderboard(error(500, "not_on_leaderboard"))).toBe(false);
    expect(isNotOnLeaderboard(undefined)).toBe(false);
  });
});
