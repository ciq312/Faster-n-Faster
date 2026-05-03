import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import { useBannerMessage, useError } from "../../../shared/components/BannerProvider";
import { apiCall } from "../../../shared/utils/apiCall";
import { useAuth } from "../AuthContext";

export function useFetchProfile() {
  const navigate = useNavigate();
  const [isPending, setIsPending] = useState(true);
  const { showError } = useError();
  const {showMessage } = useBannerMessage();
  const { userName} = useAuth();
  const [profileData, setProfileData] = useState(null);

  useEffect(() => {
    const getProfile = async () => {
      try {
        const response = await apiCall(`/api/users/profiles/me`, {
          method: "GET",
        });
        if (!response.ok) {
          console.log(response);
          setProfileData({ nick: userName });
          showMessage(`Register to see the results of your races`);
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
  }, []);

  return { profileData, isPending };
}
