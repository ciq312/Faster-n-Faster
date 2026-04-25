import { useState } from "react";
import { Link, useSearchParams } from "react-router-dom";
import { useResetPassword } from "../../features/auth/hooks/useResetPassword";
import "./ResetPassword.css";

function ResetPassword() {
  const [params] = useSearchParams();
  const token = params.get("token") ?? "";

  const [password, setPassword] = useState("");
  const [confirmPassword, setConfirmPassword] = useState("");
  const reset = useResetPassword();

  const passwordMismatch =
    confirmPassword.length > 0 && password !== confirmPassword;
  const canSubmit =
    token && password.trim() && confirmPassword.trim() && !passwordMismatch;

  const handleSubmit = async (e) => {
    e.preventDefault();
    if (!canSubmit || reset.loading) return;
    await reset.execute({ token, newPassword: password });
  };

  if (!token) {
    return (
      <div className="reset-password">
        <div className="reset-password__header">
          <h1 className="reset-password__logo">reset password</h1>
        </div>
        <div className="reset-password__card">
          <p className="reset-password__body">
            this link is invalid or expired.
          </p>
          <Link to="/forgot-password" className="reset-password__back">
            request a new link
          </Link>
        </div>
      </div>
    );
  }

  return (
    <div className="reset-password">
      <div className="reset-password__header">
        <h1 className="reset-password__logo">reset password</h1>
        <p className="reset-password__tagline">choose a new password</p>
      </div>

      <div className="reset-password__card">
        <form className="reset-password__form" onSubmit={handleSubmit}>
          <label className="reset-password__label">
            <span>new password</span>
            <input
              className="reset-password__input"
              type="password"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              placeholder="new password"
              autoFocus
            />
          </label>
          <label className="reset-password__label">
            <span>confirm password</span>
            <input
              className={`reset-password__input ${passwordMismatch ? "reset-password__input--error" : ""}`}
              type="password"
              value={confirmPassword}
              onChange={(e) => setConfirmPassword(e.target.value)}
              placeholder="repeat password"
            />
            {passwordMismatch && (
              <span className="reset-password__error">passwords don't match</span>
            )}
          </label>
          <button
            type="submit"
            className="reset-password__submit"
            disabled={!canSubmit || reset.loading}
          >
            {reset.loading ? "saving..." : "update password"}
          </button>
        </form>

        <Link to="/forgot-password" className="reset-password__back">
          request a new link
        </Link>
      </div>
    </div>
  );
}

export default ResetPassword;
