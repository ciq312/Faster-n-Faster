import { useState } from "react";
import { Link } from "react-router-dom";
import { useForgotPassword } from "../../features/auth/hooks/useForgotPassword";
import "./ForgotPassword.css";

function ForgotPassword() {
  const [email, setEmail] = useState("");
  const forgot = useForgotPassword();

  const emailInvalid =
    email.length > 0 && !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email);
  const canSubmit = email.trim() && !emailInvalid;

  const handleSubmit = (e) => {
    e.preventDefault();
    if (!canSubmit || forgot.loading || forgot.cooldown > 0) return;
    forgot.execute(email);
  };

  return (
    <div className="forgot-password">
      <div className="forgot-password__header">
        <h1 className="forgot-password__logo">forgot password</h1>
        <p className="forgot-password__tagline">we'll send you a link</p>
      </div>

      <div className="forgot-password__card">
        {!forgot.submitted && (
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
              disabled={!canSubmit || forgot.loading}
            >
              {forgot.loading ? "sending..." : "send reset link"}
            </button>
          </form>
        )}

        {forgot.submitted && (
          <>
            <p className="forgot-password__body">
              if an account with <strong>{email}</strong> exists, a reset link is on its way.
              check your inbox.
            </p>
            <button
              type="button"
              className="forgot-password__submit"
              onClick={() => forgot.execute(email)}
              disabled={forgot.loading || forgot.cooldown > 0}
            >
              {forgot.loading
                ? "sending..."
                : forgot.cooldown > 0
                  ? `resend in ${forgot.cooldown}s`
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
