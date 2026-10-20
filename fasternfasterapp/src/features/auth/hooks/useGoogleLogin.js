import { useEffect, useState } from "react";
import { API_BASE } from "../../../config";

export function useGoogleLogin() {
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    const onPageShow = (event) => {
      if (event.persisted) setLoading(false);
    };
    window.addEventListener("pageshow", onPageShow);
    return () => window.removeEventListener("pageshow", onPageShow);
  }, []);

  const execute = () => {
    setLoading(true);
    // Full-page nav: the backend drives the OAuth challenge, sets session cookies
    // on the callback, and redirects back to returnUrl. No client-side OAuth flow.
    const returnUrl = encodeURIComponent(`${window.location.origin}/lobbies`);
    window.location.href = `${API_BASE}/api/auth/google?returnUrl=${returnUrl}`;
  };

  return { execute, loading };
}
