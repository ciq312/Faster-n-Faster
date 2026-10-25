import type { ApiTag } from "./api";

export const invalidatesOnSuccess =
  (tag: ApiTag) => (_result: unknown, error: unknown) => (error ? [] : [tag]);
