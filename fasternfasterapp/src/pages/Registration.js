import { useState } from "react";
import { useNavigate } from "react-router-dom";
import LoginForm from "../components/LoginForm.js";
import SignupForm from "../components/SignupForm.js";
import "./Registration.css";

const TABS = [
  { key: "anonymous", label: "Anonymous" },
  { key: "login", label: "Log in" },
  { key: "signup", label: "Sign up" },
];

function Registration() {
  const [activeTab, setActiveTab] = useState("anonymous");
  const [nick, setNick] = useState("");

  const navigate = useNavigate();

  const handleAnonymousSubmit = async (e) => {
    e.preventDefault();
    try {
      let response = await fetch("/api/auth/guest", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ nick: nick }),
      });
      let data = await response.json();
      localStorage.setItem("token", data.token);
      localStorage.setItem("userId", data.userId);
      navigate("/lobbies");
    } catch (error) {
      console.log(error);
    }
  };

  const handleLogin = async (data) => {
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
        let errors = await response.json();
        console.log(errors.errors.generalErrors[0]);
        return;
      }
      navigate("/lobbies");
    } catch (e) {}
  };

  const handleSignup = async (data) => {
    let response = await fetch("/api/auth/register", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({
        nick: data.nick,
        login: data.login,
        password: data.password,
      }),
    });
    let responseJSON = await response.json();

    console.log(responseJSON);

    localStorage.setItem("token", responseJSON.token);
    localStorage.setItem("userId", responseJSON.userId);
    navigate("/lobbies");
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
              disabled={!nick.trim()}
            >
              play
            </button>
          </form>
        )}

        {activeTab === "login" && <LoginForm onSubmit={handleLogin} />}
        {activeTab === "signup" && <SignupForm onSubmit={handleSignup} />}
      </div>
    </div>
  );
}

export default Registration;
