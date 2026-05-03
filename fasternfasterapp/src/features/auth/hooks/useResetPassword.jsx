import { useState } from "react";
import { useNavigate } from "react-router-dom";
import { useBannerMessage, useError } from "../../../shared/components/BannerProvider";

export function useResetPassword() {
  const { showError } = useError();
  const { showMessage } = useBannerMessage();
  const navigate = useNavigate();
  const [loading, setLoading] = useState(false);

  const execute = async ({ token, newPassword }) => {
    setLoading(true);
    try {
      const response = await fetch("/api/auth/reset-password", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ token, newPassword }),
      });
      if (!response.ok) {
        showError(await extractHttpError(response));
        return { ok: false };
      }
      showMessage("password updated — please log in");
      navigate("/");
      return { ok: true };
    } catch (err) {
      showError(err.message || "Could not connect to server");
      return { ok: false };
    } finally {
      setLoading(false);
    }
  };

  return { execute, loading };
}
