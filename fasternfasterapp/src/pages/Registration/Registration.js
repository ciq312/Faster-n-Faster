import { useState } from "react";
import LoginForm from "../../features/auth/components/LoginForm";
import SignupForm from "../../features/auth/components/SignupForm";
import { useAnonymousLogin } from "../../features/auth/hooks/useAnonymousLogin";
import { useGoogleLogin } from "../../features/auth/hooks/useGoogleLogin";
import { useLogin } from "../../features/auth/hooks/useLogin";
import { useRegister } from "../../features/auth/hooks/useRegister";
import "./Registration.css";

const TABS = [
  { key: "anonymous", label: "Anonymous" },
  { key: "login", label: "Log in" },
  { key: "signup", label: "Sign up" },
];

function Registration() {
  const [activeTab, setActiveTab] = useState("anonymous");
  const [nick, setNick] = useState("");

  const anonymousLogin = useAnonymousLogin();
  const login = useLogin();
  const register = useRegister();
  const googleLogin = useGoogleLogin();

  const loading =
    anonymousLogin.loading ||
    login.loading ||
    register.loading ||
    googleLogin.loading;

  const handleAnonymousSubmit = (e) => {
    e.preventDefault();
    anonymousLogin.execute(nick);
  };

  return (
    <div className="registration">
      <div className="registration__header">
        <h1 className="registration__logo">faster'n'faster</h1>
        <p className="registration__tagline">Play hard type fast.</p>
      </div>

      <div className="registration__card">
        <div className="registration__tabs">
          {TABS.map((tab) => (
            <button
              key={tab.key}
              className={`registration__tab ${activeTab === tab.key ? "registration__tab--active" : ""}`}
              onClick={() => setActiveTab(tab.key)}
              type="button"
            >
              {tab.label}
            </button>
          ))}
        </div>

        {activeTab === "anonymous" && (
          <form className="registration__form" onSubmit={handleAnonymousSubmit}>
            <div className="registration__fields">
              <label className="registration__label">
                <span>display name</span>
                <input
                  className="registration__input"
                  type="text"
                  value={nick}
                  onChange={(e) => setNick(e.target.value)}
                  placeholder="enter your name"
                  autoFocus
                />
              </label>
            </div>
            <button
              className="registration__submit"
              type="submit"
              disabled={!nick.trim() || loading}
            >
              {loading ? "loading..." : "play"}
            </button>
          </form>
        )}

        {activeTab === "login" && (
          <LoginForm onSubmit={login.execute} loading={loading} />
        )}
        {activeTab === "signup" && (
          <SignupForm onSubmit={register.execute} loading={loading} />
        )}

        <div className="registration__divider">
          <span>or</span>
        </div>

        <div className="registration__google-wrap">
          <button
            type="button"
            className="registration__google"
            onClick={googleLogin.execute}
            disabled={loading}
          >
            continue with google
          </button>
        </div>
      </div>
    </div>
  );
}

export default Registration;
