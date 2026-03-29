import { useState, useCallback } from "react";
import { useAnonymousLogin } from "../features/auth/hooks/useAnonymousLogin";
import { useLogin } from "../features/auth/hooks/useLogin";
import { useRegister } from "../features/auth/hooks/useRegister";
import LoginForm from "../features/auth/components/LoginForm";
import SignupForm from "../features/auth/components/SignupForm";
import ErrorBanner from "../shared/components/ErrorBanner";
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

  const error = anonymousLogin.error || login.error || register.error;
  const loading = anonymousLogin.loading || login.loading || register.loading;

  const clearError = useCallback(() => {
    anonymousLogin.setError(null);
    login.setError(null);
    register.setError(null);
  }, [anonymousLogin, login, register]);

  const handleTabSwitch = (key) => {
    setActiveTab(key);
    clearError();
  };

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

      <ErrorBanner message={error} onDismiss={clearError} />

      <div className="registration__card">
        <div className="registration__tabs">
          {TABS.map((tab) => (
            <button
              key={tab.key}
              className={`registration__tab ${activeTab === tab.key ? "registration__tab--active" : ""}`}
              onClick={() => handleTabSwitch(tab.key)}
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
      </div>
    </div>
  );
}

export default Registration;
