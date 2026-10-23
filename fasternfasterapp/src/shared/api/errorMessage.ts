import type { SerializedError } from "@reduxjs/toolkit";
import type { FetchBaseQueryError } from "@reduxjs/toolkit/query";
import { apiErrorMessage } from "../utils/apiErrorMessage";

export const CONNECTION_FAILED_MESSAGE = "Could not connect to server";

export function errorMessage(
  error: FetchBaseQueryError | SerializedError | undefined,
): string {
  if (error && "status" in error) {
    if (error.status === "FETCH_ERROR") return CONNECTION_FAILED_MESSAGE;
    if (typeof error.status === "number") return apiErrorMessage(error.data);
  }
  return apiErrorMessage(null);
}
