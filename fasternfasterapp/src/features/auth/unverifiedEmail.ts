import type { SerializedError } from "@reduxjs/toolkit";
import type { FetchBaseQueryError } from "@reduxjs/toolkit/query";
import { isApiErrorResponse } from "../../shared/utils/apiErrorMessage";

const EMAIL_NOT_VERIFIED = "email_not_verified";

export function unverifiedEmail(
  error: FetchBaseQueryError | SerializedError | undefined,
): string | undefined {
  if (!error || !("status" in error) || error.status !== 403) return undefined;
  if (
    !isApiErrorResponse(error.data) ||
    error.data.code !== EMAIL_NOT_VERIFIED
  ) {
    return undefined;
  }
  return error.data.errors?.email?.[0] ?? "";
}
