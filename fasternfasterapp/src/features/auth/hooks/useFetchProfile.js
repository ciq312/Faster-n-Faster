import { useEffect, useState } from "react";
import { useError } from "../../../shared/components/BannerProvider";
import { extractError } from "../../../shared/utils/extractError";

export function useFetchProfile() {
  const [isPending, setIsPending] = useState(true);
  const { showError } = useError();
  const [profileData, setProfileData] = useState(null);

  useEffect(() => {
    const getProfile = async () => {
      try {
        const token = localStorage.getItem("token");
        const response = await fetch(`/api/users/me`, {
          method: "GET",
          headers: {
            Authorization: `Bearer ${token}`,
          },
        });
        if (!response.ok) {
          showError(await extractError(response));
          return;
        }
        const data = await response.json();
        setProfileData(data.dto);
      } catch {
        showError("Could not connect to server");
      } finally {
        setIsPending(false);
      }
    };

    getProfile();
  }, []);

  return { profileData, isPending };
}
