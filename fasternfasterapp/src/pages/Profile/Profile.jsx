import { useEffect } from "react";
import { useNavigate } from "react-router-dom";
import { useAppSelector } from "../../app/hooks";
import { useLogoutMutation } from "../../features/auth/authApi";
import {
  selectCurrentUser,
  selectIsGuest,
} from "../../features/auth/authSelectors";
import { useConnection } from "../../features/connection/ConnectionProvider";
import { useGetMyProfileQuery } from "../../features/profile/profileApi";
import { errorMessage } from "../../shared/api/errorMessage";
import {
  useBannerMessage,
  useError,
} from "../../shared/components/BannerProvider";
import Navbar from "../../shared/components/Navbar/Navbar";
import { ROUTES } from "../../shared/utils/routes";
import "./Profile.css";

function formatNumber(value, precision = 1) {
  if (typeof value !== "number") return "—";
  return Number.isInteger(value) ? value : value.toFixed(precision);
}

function formatPercent(value, precision = 2) {
  if (typeof value !== "number") return "—";
  return value.toFixed(precision);
}

function Profile() {
  const { data, isLoading, isError } = useGetMyProfileQuery();
  const currentUser = useAppSelector(selectCurrentUser);
  const isGuest = useAppSelector(selectIsGuest);
  const profileData = isError ? { nick: currentUser?.userName } : data;
  const { showMessage } = useBannerMessage();
  const { showError } = useError();
  const [logout, { isLoading: isLoggingOut }] = useLogoutMutation();
  const { disconnect } = useConnection();
  const navigate = useNavigate();

  useEffect(() => {
    if (isGuest) showMessage("Register to see the results of your races");
  }, [isGuest, showMessage]);

  const handleLogout = async () => {
    const result = await logout();
    if (result.error) {
      showError(errorMessage(result.error));
      return;
    }
    disconnect();
    navigate(ROUTES.HOME);
  };

  return (
    <div className="profile-page">
      <Navbar />
      {isGuest ? (
        <button
          className="profile-page__logout"
          onClick={() => navigate(ROUTES.HOME)}
          type="button"
        >
          register now
        </button>
      ) : (
        <button
          className="profile-page__logout"
          onClick={handleLogout}
          disabled={isLoggingOut}
          type="button"
        >
          {isLoggingOut ? "logging out..." : "log out"}
        </button>
      )}
      <div className="profile-page__content">
        {isLoading && (
          <p className="profile-page__loading">Loading profile...</p>
        )}

        {!isLoading && (
          <>
            <header className="profile-page__header">
              <span className="profile-page__eyebrow">Profile</span>
              <h1 className="profile-page__nick">{profileData?.nick}</h1>
              <p className="profile-page__meta">
                <span className="profile-page__meta-accent">
                  {profileData?.racesTyped}
                </span>{" "}
                {profileData?.racesTyped === 1 ? "race" : "races"}
                <span className="profile-page__meta-sep">·</span>
                <span className="profile-page__meta-accent">
                  {profileData?.wins}
                </span>{" "}
                {profileData?.wins === 1 ? "win" : "wins"}
              </p>
            </header>

            <section className="profile-hero">
              <span className="profile-hero__label">Best WPM</span>
              <span className="profile-hero__value">
                {formatNumber(profileData?.bestWPM)}
                <span className="profile-hero__unit" />
              </span>
            </section>

            <section className="profile-stats">
              <div className="profile-stat">
                <span className="profile-stat__label">Avg WPM</span>
                <span className="profile-stat__value">
                  {formatNumber(profileData?.avgWPM)}
                </span>
              </div>
              <div className="profile-stat">
                <span className="profile-stat__label">Best Accuracy</span>
                <span className="profile-stat__value">
                  {formatPercent(profileData?.bestAccuracy)}
                  <span className="profile-stat__suffix">%</span>
                </span>
              </div>
              <div className="profile-stat">
                <span className="profile-stat__label">Avg Accuracy</span>
                <span className="profile-stat__value">
                  {formatPercent(profileData?.avgAccuracy)}
                  <span className="profile-stat__suffix">%</span>
                </span>
              </div>
              <div className="profile-stat">
                <span className="profile-stat__label">Words Typed</span>
                <span className="profile-stat__value">
                  {formatNumber(profileData?.wordsTyped)}
                </span>
              </div>
              <div className="profile-stat">
                <span className="profile-stat__label">Races Typed</span>
                <span className="profile-stat__value">
                  {formatNumber(profileData?.racesTyped)}
                </span>
              </div>
              <div className="profile-stat">
                <span className="profile-stat__label">Wins</span>
                <span className="profile-stat__value">
                  {formatNumber(profileData?.wins)}
                </span>
              </div>
            </section>
          </>
        )}
      </div>
    </div>
  );
}

export default Profile;
