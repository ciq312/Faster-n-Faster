import { API_BASE } from "../../config";

let activeRefresh = null;

export function refreshOnce() {
  activeRefresh ??= fetch(`${API_BASE}/api/auth/refresh`, {
    method: "POST",
    credentials: "include",
  }).finally(() => {
    activeRefresh = null;
  });
  return activeRefresh;
}

