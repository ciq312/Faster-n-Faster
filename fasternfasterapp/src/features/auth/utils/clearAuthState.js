// Removes every localStorage key the auth flow writes. Called on account switch to avoid
// leaving stale identity fields under a freshly-issued token.
const AUTH_LOCALSTORAGE_KEYS = ["userId", "userName"];

export function clearAuthState() {
  AUTH_LOCALSTORAGE_KEYS.forEach((key) => localStorage.removeItem(key));
}
