import type { ApiErrorResponse } from "../../../types";

const FALLBACK_MESSAGE = "Something went wrong, try again";
const VALIDATION_FAILED = "validation_failed";

export async function extractHttpError(response: Response): Promise<string> {
  try {
    const body: ApiErrorResponse | null = await response.json();
    return firstValidationMessage(body) ?? (body?.reason || FALLBACK_MESSAGE);
  } catch {
    return FALLBACK_MESSAGE;
  }
}

// `errors` holds field messages only for validation failures; other codes put data there (e.g. the email).
function firstValidationMessage(
  body: ApiErrorResponse | null,
): string | undefined {
  if (body?.code !== VALIDATION_FAILED) return undefined;
  return Object.values(body.errors ?? {}).flat()[0];
}
