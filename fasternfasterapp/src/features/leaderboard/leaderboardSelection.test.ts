import { describe, expect, it } from "vitest";
import { parseSelection, toSearchParams } from "./leaderboardSelection";

const defaults = { sort: "BestWpm", descending: true, page: 1 };

function parse(query: string) {
  return parseSelection(new URLSearchParams(query));
}

describe("leaderboard selection", () => {
  it("reads sort, direction and page from the URL", () => {
    expect(parse("sort=Wins&desc=false&page=4")).toEqual({
      sort: "Wins",
      descending: false,
      page: 4,
    });
  });

  it("falls back to Best WPM descending page 1 when params are missing", () => {
    expect(parse("")).toEqual(defaults);
  });

  it("falls back on an unknown sort", () => {
    expect(parse("sort=Bogus&desc=false&page=2").sort).toBe("BestWpm");
  });

  it("falls back on a page that is not a positive integer", () => {
    for (const page of ["0", "-1", "1.5", "abc", ""]) {
      expect(parse(`page=${page}`).page).toBe(1);
    }
  });

  it("falls back on a desc value other than true or false", () => {
    expect(parse("desc=1").descending).toBe(true);
  });

  it("writes all three params", () => {
    const params = toSearchParams({ sort: "Wins", descending: false, page: 3 });

    expect(params.toString()).toBe("sort=Wins&desc=false&page=3");
  });
});
