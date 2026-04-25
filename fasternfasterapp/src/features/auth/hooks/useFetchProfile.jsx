import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import { useError } from "../../../shared/components/BannerProvider";
import { extractError } from "../../../shared/utils/extractError";

export function useFetchProfile() {
  const navigate = useNavigate();
  const [isPending, setIsPending] = useState(true);
  const { showError } = useError();
  const [profileData, setProfileData] = useState(null);

  useEffect(() => {
    const controller = new AbortController();
    const getProfile = async () => {
      try {
        console.log(`getting profile`);
        const token = localStorage.getItem("token");
        const response = await fetch(`/api/users/profiles/me`, {
          method: "GET",
          headers: {
            Authorization: `Bearer ${token}`,
          },
          signal: controller.signal,
        });
        if (!response.ok) {
          showError(await extractError(response));
          return;
        }
        const data = await response.json();
        setProfileData(data.dto);
      } catch {
        if (!controller.signal.aborted) {
          showError("Could not connect to server");
          navigate("/");
        }
      } finally {
        if (!controller.signal.aborted) {
          setIsPending(false);
        }
      }
    };

    getProfile();

    return () => controller.abort();
  }, []);

  return { profileData, isPending };
}
