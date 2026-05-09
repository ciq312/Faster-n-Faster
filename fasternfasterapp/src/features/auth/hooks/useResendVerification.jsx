import { useEffect, useState } from "react";
import { useError } from "../../../shared/components/BannerProvider";
import { API_BASE } from "../../../shared/utils/apiCall";

const COOLDOWN_SECONDS = 15;

export function useResendVerification() {
  const { showError } = useError();
  const [loading, setLoading] = useState(false);
  const [cooldown, setCooldown] = useState(15);

  useEffect(() => {
    if (cooldown <= 0) return;
    const id = setTimeout(() => setCooldown((c) => c - 1), 1000);
    return () => clearTimeout(id);
  }, [cooldown]);

  const execute = async (email) => {
    if (!email || loading || cooldown > 0) return;
    setLoading(true);
    try {
      await fetch(`${API_BASE}/api/auth/resend-verification`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ email }),
      });
      setCooldown(COOLDOWN_SECONDS);
    } catch (err) {
      showError(err.message || "Could not connect to server");
    } finally {
      setLoading(false);
    }
  };

  return { execute, loading, cooldown };
}
