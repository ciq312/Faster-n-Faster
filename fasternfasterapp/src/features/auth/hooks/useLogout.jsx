import { useState } from "react";
import { useNavigate } from "react-router-dom";
import { useConnection } from "../../connection/ConnectionProvider";
import { useAuth } from "../AuthContext";
import { clearAuthState } from "../utils/clearAuthState";
import { API_BASE } from "../../../shared/utils/apiCall";

export function useLogout() {
  const { clear } = useAuth();
  // Connection context only exists inside ConnectionLayout — guard so this hook
  // is also safe to call from pages outside that layout.
  const connection = useConnection();
  const navigate = useNavigate();
  const [loading, setLoading] = useState(false);

  const execute = async () => {
    setLoading(true);
    try {
      await fetch(`${API_BASE}/api/auth/logout`, {
        method: "POST",
        credentials: "include",
      });
    } finally {
      // Run regardless of network outcome — user should end up logged out client-side
      // even if the request failed, otherwise they'd be stuck.
      connection?.disconnect?.();
      clear();
      clearAuthState();
      setLoading(false);
      navigate("/");
    }
  };

  return { execute, loading };
}
