import { useEffect, useState } from "react";
import { useBannerMessage } from "../../../shared/components/BannerProvider";
import { apiCall } from "../../../shared/utils/apiCall";
import { useAuth } from "../AuthContext";

export function useFetchProfile() {
  const [isPending, setIsPending] = useState(true);
  const {showMessage } = useBannerMessage();
  const { userName, isGuest } = useAuth();
  const [profileData, setProfileData] = useState(null);

  useEffect(() => {
    const getProfile = async () => {
      try {
        const response = await apiCall(`/api/users/me/profile`, {
          method: "GET",
        });
        if (!response.ok) {
          setProfileData({ nick: userName });
          }
        else {
        const data = await response.json();
        setProfileData(data.dto);
        }
      } finally {
        setIsPending(false);
      }
      
    };

    getProfile();
  }, [userName]);

  useEffect(() => {
      if (isGuest) showMessage(`Register to see the results of your races`);
  }, [isGuest, showMessage]);

  return { profileData, isPending };
}
