import { useFetchProfile } from "../../features/auth/hooks/useFetchProfile";
import Navbar from "../../shared/components/Navbar/Navbar";
import "./Profile.css";

function formatNumber(value, precision = 1) {
  if (typeof value !== "number") return "—";
  return Number.isInteger(value) ? value : value.toFixed(precision);
}

function formatPercent(value, precision = 2) {
  if (typeof value !== "number") return "—";
  return (value * 100).toFixed(precision);
}

function Profile() {
  const { profileData, isPending } = useFetchProfile();

  return (
    <div className="profile-page">
      <Navbar />
      <div className="profile-page__content">
        {isPending && (
          <p className="profile-page__loading">Loading profile...</p>
        )}

        {!isPending && (
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
                {(formatNumber(profileData?.bestWPM))}
                <span className="profile-hero__unit"></span>
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
