import { describe, expect, it } from "vitest";
import { pageItems } from "./pageItems";

describe("pageItems", () => {
  it("shows first, last and the pages around the current one", () => {
    expect(pageItems(5, 9)).toEqual([1, "gap", 4, 5, 6, "gap", 9]);
  });

  it("puts a gap between distant pages", () => {
    expect(pageItems(1, 20)).toEqual([1, 2, "gap", 20]);
  });

  it("shows the page itself instead of a gap that hides only one page", () => {
    expect(pageItems(4, 8)).toEqual([1, 2, 3, 4, 5, "gap", 8]);
  });

  it("shows a single page without gaps", () => {
    expect(pageItems(1, 1)).toEqual([1]);
  });

  it("shows no pages when there are none", () => {
    expect(pageItems(1, 0)).toEqual([]);
  });
});
