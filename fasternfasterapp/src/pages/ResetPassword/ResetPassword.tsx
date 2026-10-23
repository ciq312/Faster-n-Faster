import { useState, type FormEvent } from "react";
import { Link, useNavigate, useSearchParams } from "react-router-dom";
import { useResetPasswordMutation } from "../../features/auth/authApi";
import { errorMessage } from "../../shared/api/errorMessage";
import {
  useBannerMessage,
  useError,
} from "../../shared/components/BannerProvider";
import { ROUTES } from "../../shared/utils/routes";
import "./ResetPassword.css";

function ResetPassword() {
  const [params] = useSearchParams();
  const token = params.get("token") ?? "";

  const [password, setPassword] = useState("");
  const [confirmPassword, setConfirmPassword] = useState("");
  const { showError } = useError();
  const { showMessage } = useBannerMessage();
  const navigate = useNavigate();
  const [resetPassword, { isLoading }] = useResetPasswordMutation();

  const passwordMismatch =
    confirmPassword.length > 0 && password !== confirmPassword;
  const canSubmit =
    token && password.trim() && confirmPassword.trim() && !passwordMismatch;

  const handleSubmit = async (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    if (!canSubmit || isLoading) return;
    const result = await resetPassword({ token, newPassword: password });
    if (result.error) {
      showError(errorMessage(result.error));
      return;
    }
    showMessage("password updated — please log in");
    navigate(ROUTES.HOME);
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
              <span className="reset-password__error">
                passwords don&apos;t match
              </span>
            )}
          </label>
          <button
            type="submit"
            className="reset-password__submit"
            disabled={!canSubmit || isLoading}
          >
            {isLoading ? "saving..." : "update password"}
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
