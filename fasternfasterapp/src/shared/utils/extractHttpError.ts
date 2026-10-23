import { apiErrorMessage } from "./apiErrorMessage";

export async function extractHttpError(response: Response): Promise<string> {
  const body = await response.json().catch(() => null);
  return apiErrorMessage(body);
}
