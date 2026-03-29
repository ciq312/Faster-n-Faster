import { useState, useEffect, useCallback } from "react";
import { extractError } from "../../../shared/utils/extractError";

export function useFetchLobbies() {
  const [lobbies, setLobbies] = useState([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState(null);

  const fetchLobbies = useCallback(async () => {
    setLoading(true);
    setLobbies([]);
    setError(null);
    setTimeout(async () => {
      try {
        const response = await fetch("/api/lobbies");
        if (!response.ok) {
          setError(await extractError(response));
          setLoading(false);
          return;
        }
        const data = await response.json();
        setLobbies(data.lobbies);
      } catch {
        setError("Could not connect to server");
      } finally {
        setLoading(false);
      }
    }, 500);
  }, []);

  useEffect(() => {
    fetchLobbies();
  }, [fetchLobbies]);

  return { lobbies, loading, error, setError, refresh: fetchLobbies };
}
