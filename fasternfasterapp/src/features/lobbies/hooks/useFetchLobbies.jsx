import { useCallback, useEffect, useState } from "react";
import { useError } from "../../../shared/components/BannerProvider";
import { extractHttpError } from "../../../shared/utils/extractHttpError";

export function useFetchLobbies() {
  const { showError } = useError();
  const [lobbies, setLobbies] = useState([]);
  const [loading, setLoading] = useState(false);

  const fetchLobbies = useCallback(async () => {
    setLoading(true);
    setLobbies([]);
    setTimeout(async () => {
      try {
        const response = await fetch("/api/lobbies");
        if (!response.ok) {
          showError(await extractHttpError(response));
          setLoading(false);
          return;
        }
        const data = await response.json();
        setLobbies(data.lobbies);
      } catch (e) {
        showError(e.message);
      } finally {
        setLoading(false);
      }
    }, 500);
  }, []);

  useEffect(() => {
    fetchLobbies();
  }, [fetchLobbies]);

  return { lobbies, loading, refresh: fetchLobbies };
}
