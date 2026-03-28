import { useState, useCallback } from "react";
import { useNavigate } from "react-router-dom";
import LoginForm from "../components/LoginForm.js";
import SignupForm from "../components/SignupForm.js";
import ErrorBanner from "../components/ErrorBanner.js";
import "./Registration.css";

const TABS = [
  { key: "anonymous", label: "Anonymous" },
  { key: "login", label: "Log in" },
  { key: "signup", label: "Sign up" },
];

async function extractError(response) {
  try {
    const body = await response.json();
    if (body.errors) {
      const firstKey = Object.keys(body.errors)[0];
      if (firstKey && body.errors[firstKey]?.length)
        return body.errors[firstKey][0];
    }
    return body.message || `Error ${response.status}`;
  } catch {
    return "Something went wrong, try again";
  }
}

function Registration() {
  const [activeTab, setActiveTab] = useState("anonymous");
  const [nick, setNick] = useState("");
  const [error, setError] = useState(null);
  const [loading, setLoading] = useState(false);

  const navigate = useNavigate();
  const clearError = useCallback(() => setError(null), []);

  const handleTabSwitch = (key) => {
    setActiveTab(key);
    setError(null);
  };

  const handleAnonymousSubmit = async (e) => {
    e.preventDefault();
    setError(null);
    setLoading(true);
    try {
      let response = await fetch("/api/auth/guest", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ nick: nick }),
      });
      if (!response.ok) {
        setError(await extractError(response));
        return;
      }
      let data = await response.json();
      localStorage.setItem("token", data.token);
      localStorage.setItem("userId", data.userId);
      navigate("/lobbies");
    } catch {
      setError("Could not connect to server");
    } finally {
      setLoading(false);
    }
  };

  const handleLogin = async (data) => {
    setError(null);
    setLoading(true);
    try {
      let response = await fetch("/api/auth/login", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          login: data.login,
          password: data.password,
        }),
      });
      if (!response.ok) {
        setError(await extractError(response));
        return;
      }
      let responseJSON = await response.json();
      localStorage.setItem("token", responseJSON.token);
      localStorage.setItem("userId", responseJSON.userId);
      navigate("/lobbies");
    } catch {
      setError("Could not connect to server");
    } finally {
      setLoading(false);
    }
  };

  const handleSignup = async (data) => {
    setError(null);
    setLoading(true);
    try {
      let response = await fetch("/api/auth/register", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          nick: data.nick,
          login: data.login,
          password: data.password,
        }),
      });
      if (!response.ok) {
        setError(await extractError(response));
        return;
      }
      let responseJSON = await response.json();
      localStorage.setItem("token", responseJSON.token);
      localStorage.setItem("userId", responseJSON.userId);
      navigate("/lobbies");
    } catch {
      setError("Could not connect to server");
    } finally {
      setLoading(false);
    }
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
          <LoginForm onSubmit={handleLogin} loading={loading} />
        )}
        {activeTab === "signup" && (
          <SignupForm onSubmit={handleSignup} loading={loading} />
        )}
      </div>
    </div>
  );
}

export default Registration;
