export type PageItem = number | "gap";

export function pageItems(current: number, total: number): PageItem[] {
  if (total < 1) return [];

  const pages = [...new Set([1, current - 1, current, current + 1, total])]
    .filter((page) => page >= 1 && page <= total)
    .sort((a, b) => a - b);

  return pages.flatMap((page, i) => {
    const previous = pages[i - 1];
    if (previous === undefined || page - previous === 1) return [page];
    return page - previous === 2 ? [previous + 1, page] : ["gap", page];
  });
}
