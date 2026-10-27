import type { GetLobbiesResult, LobbyListItem } from "../../../types";

export function uniqueLobbies(pages: GetLobbiesResult[]): LobbyListItem[] {
  const seen = new Set<string | undefined>();
  return pages
    .flatMap((page) => page.items ?? [])
    .filter((lobby) => {
      if (seen.has(lobby.id)) return false;
      seen.add(lobby.id);
      return true;
    });
}
