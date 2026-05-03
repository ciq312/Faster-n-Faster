import { useState } from "react";
import { useNavigate } from "react-router-dom";
import { useError } from "../../../shared/components/BannerProvider";
import { extractHttpError } from "../../../shared/utils/extractHttpError";
import { useAuth } from "../AuthContext";

export function useLogin() {
  const { showError } = useError();
  const { refresh } = useAuth();
  const [loading, setLoading] = useState(false);
  const navigate = useNavigate();

  const execute = async ({ login, password }) => {
    setLoading(true);
    try {
      const response = await fetch("/api/auth/login", {
        method: "POST",
        credentials: "include",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ login, password }),
      });
      if (response.status === 403) {
        const body = await response
          .clone()
          .json()
          .catch(() => null);
        if (body?.code === "EMAIL_NOT_VERIFIED") {
          navigate(
            `/check-your-email?email=${encodeURIComponent(body.email ?? login)}`,
          );
          return;
        }
      }
      if (!response.ok) {
        showError(await extractHttpError(response));
        return;
      }
      await refresh();
      navigate("/lobbies");
    } catch (err) {
      showError(err.message || "Could not connect to server");
    } finally {
      setLoading(false);
    }
  };

  return { execute, loading };
}
