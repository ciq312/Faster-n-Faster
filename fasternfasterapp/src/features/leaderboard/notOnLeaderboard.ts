import type { SerializedError } from "@reduxjs/toolkit";
import type { FetchBaseQueryError } from "@reduxjs/toolkit/query";
import { isApiErrorResponse } from "../../shared/utils/apiErrorMessage";

const NOT_ON_LEADERBOARD = "not_on_leaderboard";

export function isNotOnLeaderboard(
  error: FetchBaseQueryError | SerializedError | undefined,
): boolean {
  return (
    !!error &&
    "status" in error &&
    error.status === 404 &&
    isApiErrorResponse(error.data) &&
    error.data.code === NOT_ON_LEADERBOARD
  );
}
