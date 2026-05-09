import { useState } from "react";
import { useNavigate } from "react-router-dom";
import { useError } from "../../../shared/components/BannerProvider";
import { extractHttpError } from "../../../shared/utils/extractHttpError";
import { API_BASE } from "../../../shared/utils/apiCall";

export function useRegister() {
  const { showError } = useError();
  const navigate = useNavigate();
  const [loading, setLoading] = useState(false);

  const execute = async ({ nick, login, email, password }) => {
    setLoading(true);
    try {
      const response = await fetch(`${API_BASE}/api/auth/register`, {
        method: "POST",
        credentials: "include",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ nick, login, email, password }),
      });
      if (!response.ok) {
        showError(await extractHttpError(response));
        return;
      }
      navigate(`/check-your-email?email=${encodeURIComponent(email)}`);
    } catch (err) {
      showError(err.message || "Could not connect to server");
    } finally {
      setLoading(false);
    }
  };

  return { execute, loading };
}
