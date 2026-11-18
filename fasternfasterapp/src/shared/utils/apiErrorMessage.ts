import type { ApiErrorResponse } from "../../../types";

export const FALLBACK_MESSAGE = "Something went wrong, try again";
const VALIDATION_FAILED = "validation_failed";

export function isApiErrorResponse(body: unknown): body is ApiErrorResponse {
  return (
    typeof body === "object" &&
    body !== null &&
    "code" in body &&
    typeof body.code === "string"
  );
}

export function apiErrorMessage(body: unknown): string {
  if (!isApiErrorResponse(body)) return FALLBACK_MESSAGE;
  return firstValidationMessage(body) ?? (body.reason || FALLBACK_MESSAGE);
}

// `errors` holds field messages only for validation failures; other codes put data there (e.g. the email).
function firstValidationMessage(body: ApiErrorResponse): string | undefined {
  if (body.code !== VALIDATION_FAILED) return undefined;
  return Object.values(body.errors ?? {}).flat()[0];
}
