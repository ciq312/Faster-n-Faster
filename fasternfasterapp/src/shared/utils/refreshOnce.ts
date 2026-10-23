import { API_BASE } from "../../config";

let activeRefresh: Promise<Response> | null = null;

export function refreshOnce(): Promise<Response> {
  activeRefresh ??= fetch(`${API_BASE}/api/auth/refresh`, {
    method: "POST",
    credentials: "include",
  }).finally(() => {
    activeRefresh = null;
  });
  return activeRefresh;
}
