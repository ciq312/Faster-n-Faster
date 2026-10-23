import { useEffect, useRef, useState, type FormEvent } from "react";
import { useNavigate } from "react-router-dom";
import type { LoginUserRequest, RegisterUserRequest } from "../../../types";
import { useAppSelector } from "../../app/hooks";
import {
  useGuestMutation,
  useLoginMutation,
  useRegisterMutation,
} from "../../features/auth/authApi";
import { selectAuthStatus } from "../../features/auth/authSelectors";
import LoginForm from "../../features/auth/components/LoginForm";
import SignupForm from "../../features/auth/components/SignupForm";
import { useGoogleLogin } from "../../features/auth/hooks/useGoogleLogin";
import { unverifiedEmail } from "../../features/auth/unverifiedEmail";
import { errorMessage } from "../../shared/api/errorMessage";
import { useError } from "../../shared/components/BannerProvider";
import { ROUTES } from "../../shared/utils/routes";
import "./Registration.css";

const TABS = [
  { key: "anonymous", label: "Anonymous" },
  { key: "login", label: "Log in" },
  { key: "signup", label: "Sign up" },
];

const checkYourEmailPath = (email: string) =>
  `/check-your-email?email=${encodeURIComponent(email)}`;

function Registration() {
  const [activeTab, setActiveTab] = useState("anonymous");
  const [nick, setNick] = useState("");
  const status = useAppSelector(selectAuthStatus);
  const { showError } = useError();
  const navigate = useNavigate();
  // Only redirect on the loading → authenticated transition (i.e. /me just resolved).
  // If Registration is mounted with status already "authenticated", the user got here
  // intentionally (e.g. clicked the logo to switch accounts) — leave them on the form.
  const prevStatusRef = useRef(status);

  useEffect(() => {
    if (prevStatusRef.current === "loading" && status === "authenticated") {
      navigate(ROUTES.LOBBIES, { replace: true });
    }
    prevStatusRef.current = status;
  }, [status, navigate]);

  const [guest, guestState] = useGuestMutation();
  const [login, loginState] = useLoginMutation();
  const [register, registerState] = useRegisterMutation();
  const googleLogin = useGoogleLogin();

  const loading =
    guestState.isLoading ||
    loginState.isLoading ||
    registerState.isLoading ||
    googleLogin.loading;

  const handleAnonymousSubmit = async (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    const result = await guest({ nick });
    if (result.error) showError(errorMessage(result.error));
    else navigate(ROUTES.LOBBIES);
  };

  const handleLogin = async (credentials: LoginUserRequest) => {
    const result = await login(credentials);
    if (!result.error) {
      navigate(ROUTES.LOBBIES);
      return;
    }
    const email = unverifiedEmail(result.error);
    if (email === undefined) showError(errorMessage(result.error));
    else navigate(checkYourEmailPath(email));
  };

  const handleRegister = async (form: RegisterUserRequest) => {
    const result = await register(form);
    if (result.error) showError(errorMessage(result.error));
    else navigate(checkYourEmailPath(form.email));
  };

  return (
    <div className="registration">
      <div className="registration__header">
        <h1 className="registration__logo">faster&apos;n&apos;faster</h1>
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
          <LoginForm onSubmit={handleLogin} loading={loading} />
        )}
        {activeTab === "signup" && (
          <SignupForm onSubmit={handleRegister} loading={loading} />
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

      <footer className="registration__footer">
        <span>© {new Date().getFullYear()} faster&apos;n&apos;faster</span>
        <span className="registration__footer-sep">·</span>
        <a href="mailto:lesha.mak34@gmail.com">contact</a>
      </footer>
    </div>
  );
}

export default Registration;
