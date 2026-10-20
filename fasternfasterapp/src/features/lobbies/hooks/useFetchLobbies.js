import { useCallback, useEffect, useState } from "react";
import { useError } from "../../../shared/components/BannerProvider";
import { extractHttpError } from "../../../shared/utils/extractHttpError";
import { API_BASE } from "../../../config";

export function useFetchLobbies() {
  const { showError } = useError();
  const [lobbies, setLobbies] = useState([]);
  const [loading, setLoading] = useState(false);

  const fetchLobbies = useCallback(async () => {
    setLoading(true);
    setLobbies([]);
    try {
      const response = await fetch(`${API_BASE}/api/lobbies`);
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
  }, [showError]);

  useEffect(() => {
    fetchLobbies();
  }, [fetchLobbies]);

  return { lobbies, loading, refresh: fetchLobbies };
}
