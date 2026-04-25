import { useState } from "react";
import { useNavigate } from "react-router-dom";
import { useError } from "../../../shared/components/BannerProvider";
import { extractError } from "../../../shared/utils/extractError";
import { useAuth } from "../AuthContext";

export function useAnonymousLogin() {
  const { showError } = useError();
  const { refresh } = useAuth();
  const [loading, setLoading] = useState(false);
  const navigate = useNavigate();

  const execute = async (nick) => {
    setLoading(true);
    try {
      const response = await fetch("/api/auth/guest", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ nick }),
      });
      if (!response.ok) {
        showError(await extractError(response));
        return;
      }
      await refresh();
      navigate("/lobbies");
    } catch {
      showError("Could not connect to server");
    } finally {
      setLoading(false);
    }
  };

  return { execute, loading };
}
