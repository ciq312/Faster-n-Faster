import { describe, expect, it } from "vitest";
import type { GetLobbiesResult, LobbyListItem } from "../../../types";
import { uniqueLobbies } from "./uniqueLobbies";

function lobby(id: string): LobbyListItem {
  return { id, name: id };
}

function page(...ids: string[]): GetLobbiesResult {
  return { items: ids.map(lobby) };
}

describe("uniqueLobbies", () => {
  it("flattens pages in order", () => {
    const lobbies = uniqueLobbies([page("a", "b"), page("c")]);

    expect(lobbies.map((l) => l.id)).toEqual(["a", "b", "c"]);
  });

  it("keeps the first copy of a lobby that appears on two pages", () => {
    const first = { ...lobby("b"), name: "first copy" };
    const second = { ...lobby("b"), name: "second copy" };

    const lobbies = uniqueLobbies([
      { items: [lobby("a"), first] },
      { items: [second, lobby("c")] },
    ]);

    expect(lobbies).toEqual([lobby("a"), first, lobby("c")]);
  });

  it("treats a page without items as empty", () => {
    expect(uniqueLobbies([{}, page("a")])).toEqual([lobby("a")]);
  });
});
