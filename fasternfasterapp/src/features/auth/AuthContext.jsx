import { createContext, useCallback, useContext, useEffect, useState } from "react";
import { apiCall } from "../../shared/utils/apiCall";

const AuthContext = createContext(null);

export function AuthProvider({ children }) {
  const [userId, setUserId] = useState(null);
  const [userName, setUserName] = useState(null);
  const [status, setStatus] = useState("loading");

  const refresh = useCallback(async () => {
    try {
      const response = await apiCall("/api/auth/me");
      if (!response.ok) {
        setUserId(null);
        setUserName(null);
        setStatus("not authenticated");
        return;
      }
      const data = await response.json();
      setUserId(data.userId);
      setUserName(data.userName);
      setStatus("authenticated");
    } catch {
      setUserId(null);
      setUserName(null);
      setStatus("not authenticated");
    }
  }, []);

  const clear = useCallback(() => {
    setUserId(null);
    setUserName(null);
    setStatus("not authenticated");
  }, []);

  useEffect(() => {
    refresh();
  }, [refresh]);

  return (
    <AuthContext.Provider value={{ userId, userName, status, refresh, clear }}>
      {children}
    </AuthContext.Provider>
  );
}

export function useAuth() {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error("useAuth must be used inside <AuthProvider>");
  return ctx;
}
