import { useState } from "react";

export function useGoogleLogin() {
  const [loading, setLoading] = useState(false);

  const execute = () => {
    setLoading(true);
    // Full-page nav: the backend drives the OAuth challenge, sets session cookies
    // on the callback, and redirects back to returnUrl. No client-side OAuth flow.
    const returnUrl = encodeURIComponent(`${window.location.origin}/lobbies`);
    window.location.href = `/api/auth/google?returnUrl=${returnUrl}`;
  };

  return { execute, loading };
}
