import { useState, type FormEvent } from "react";
import { Link } from "react-router-dom";
import { useForgotPasswordMutation } from "../../features/auth/authApi";
import { errorMessage } from "../../shared/api/errorMessage";
import { useError } from "../../shared/components/BannerProvider";
import { useCooldown } from "../../shared/hooks/useCooldown";
import "./ForgotPassword.css";

const COOLDOWN_SECONDS = 15;

function ForgotPassword() {
  const [email, setEmail] = useState("");
  const [submitted, setSubmitted] = useState(false);
  const { showError } = useError();
  const { cooldown, start } = useCooldown(COOLDOWN_SECONDS);
  const [forgotPassword, { isLoading }] = useForgotPasswordMutation();

  const emailInvalid =
    email.length > 0 && !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email);
  const canSubmit = email.trim() && !emailInvalid;

  const send = async () => {
    const result = await forgotPassword({ email });
    if (result.error) {
      showError(errorMessage(result.error));
      return;
    }
    setSubmitted(true);
    start();
  };

  const handleSubmit = (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    if (!canSubmit || isLoading || cooldown > 0) return;
    send();
  };

  return (
    <div className="forgot-password">
      <div className="forgot-password__header">
        <h1 className="forgot-password__logo">forgot password</h1>
        <p className="forgot-password__tagline">we&apos;ll send you a link</p>
      </div>

      <div className="forgot-password__card">
        {!submitted && (
          <form className="forgot-password__form" onSubmit={handleSubmit}>
            <label className="forgot-password__label">
              <span>email</span>
              <input
                className={`forgot-password__input ${emailInvalid ? "forgot-password__input--error" : ""}`}
                type="email"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                placeholder="you@example.com"
                autoFocus
              />
              {emailInvalid && (
                <span className="forgot-password__error">invalid email</span>
              )}
            </label>
            <button
              type="submit"
              className="forgot-password__submit"
              disabled={!canSubmit || isLoading}
            >
              {isLoading ? "sending..." : "send reset link"}
            </button>
          </form>
        )}

        {submitted && (
          <>
            <p className="forgot-password__body">
              if an account with <strong>{email}</strong> exists, a reset link
              is on its way. check your inbox.
            </p>
            <button
              type="button"
              className="forgot-password__submit"
              onClick={send}
              disabled={isLoading || cooldown > 0}
            >
              {isLoading
                ? "sending..."
                : cooldown > 0
                  ? `resend in ${cooldown}s`
                  : "resend link"}
            </button>
          </>
        )}

        <Link to="/" className="forgot-password__back">
          back to login
        </Link>
      </div>
    </div>
  );
}

export default ForgotPassword;
