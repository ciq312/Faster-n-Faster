import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useState,
} from "react";
import { apiCall } from "../../shared/utils/apiCall";

const AuthContext = createContext(null);

export function AuthProvider({ children }) {
  const [userId, setUserId] = useState(null);
  const [userName, setUserName] = useState(null);
  const [role, setRole] = useState(null);
  const [status, setStatus] = useState("loading");

  const refresh = useCallback(async () => {
    try {
      const response = await apiCall("/api/auth/me");
      if (!response.ok) {
        setUserId(null);
        setUserName(null);
        setRole(null);
        setStatus("not authenticated");
        return;
      }
      const data = await response.json();
            console.log(data);
      setUserId(data.userId);
      setUserName(data.userName);
      setRole(data.role);
      setStatus("authenticated");
    } catch {
      setUserId(null);
      setUserName(null);
      setRole(null);
      setStatus("not authenticated");
    }
  }, []);

  const clear = useCallback(() => {
    setUserId(null);
    setUserName(null);
    setRole(null);
    setStatus("not authenticated");
  }, []);

  const isSelf = useCallback((id) => userId === id, [userId]);
  const isGuest = role === "Guest";

  useEffect(() => {
    refresh();
  }, [refresh]);

  return (
    <AuthContext.Provider value={{ userId, userName, role, isGuest, status, refresh, clear, isSelf }}>
      {children}
    </AuthContext.Provider>
  );
}

export function useAuth() {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error("useAuth must be used inside <AuthProvider>");
  return ctx;
}
