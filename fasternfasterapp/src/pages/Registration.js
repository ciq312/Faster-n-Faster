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
      let response = await fetch("/api/users/anonymous", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ nick: nick }),
      });
      let data = await response.json();
      localStorage.setItem("token", data.token);
      navigate("/lobbies");
    } catch (error) {
      console.log(error);
    }
    // navigate("/lobbies");
    // try {
    //   let response = await fetch("/api/users/anonymous", {
    //     method: "POST",
    //     headers: { "Content-Type": "application/json" },
    //     body: JSON.stringify(nick),
    //   });
    //   console.log(response);
    // } catch (e) {
    //   console.log(e);
    // }

    // placeholder — will navigate to lobbies with display name
  };

  const handleLogin = async (data) => {
    // placeholder — { login, password }
  };

  const handleSignup = async (data) => {
    // placeholder — { login, nick, password }
  };

  return (
    <div className="registration">
      <h1 className="registration__logo">faster'n'faster</h1>

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
