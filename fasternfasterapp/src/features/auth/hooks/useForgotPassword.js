import { useEffect, useState } from "react";
import { useError } from "../../../shared/components/BannerProvider";

const COOLDOWN_SECONDS = 15;

export function useForgotPassword() {
  const { showError } = useError();
  const [loading, setLoading] = useState(false);
  const [submitted, setSubmitted] = useState(false);
  const [cooldown, setCooldown] = useState(0);

  useEffect(() => {
    if (cooldown <= 0) return;
    const id = setTimeout(() => setCooldown((c) => c - 1), 1000);
    return () => clearTimeout(id);
  }, [cooldown]);

  const execute = async (email) => {
    if (!email || loading || cooldown > 0) return;
    setLoading(true);
    try {
      // Server always returns 200 regardless of whether the email exists (no enumeration).
      await fetch("/api/auth/forgot-password", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ email }),
      });
      setSubmitted(true);
      setCooldown(COOLDOWN_SECONDS);
    } catch (err) {
      showError(err.message || "Could not connect to server");
    } finally {
      setLoading(false);
    }
  };

  return { execute, loading, cooldown, submitted };
}
