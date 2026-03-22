import { useState } from "react";

function SignupForm({ onSubmit }) {
  const [login, setLogin] = useState("");
  const [nick, setNick] = useState("");
  const [password, setPassword] = useState("");
  const [confirmPassword, setConfirmPassword] = useState("");

  const passwordMismatch =
    confirmPassword.length > 0 && password !== confirmPassword;
  const canSubmit =
    login.trim() &&
    nick.trim() &&
    password.trim() &&
    confirmPassword.trim() &&
    !passwordMismatch;

  const handleSubmit = async (e) => {
    e.preventDefault();
    if (!canSubmit) return;
    const errors = await onSubmit({ login, nick, password });
    if (errors) handleServerErrors(errors);
  };

  const handleServerErrors = (errors) => {

  };

  return (
    <form className="registration__form" onSubmit={handleSubmit}>
      <div className="registration__fields">
        <label className="registration__label">
          <span>login</span>
          <input
            className="registration__input"
            type="text"
            value={login}
            onChange={(e) => setLogin(e.target.value)}
            placeholder="your username"
            autoFocus
          />
        </label>
        <label className="registration__label">
          <span>nick</span>
          <input
            className="registration__input"
            type="text"
            value={nick}
            onChange={(e) => setNick(e.target.value)}
            placeholder="display name"
          />
        </label>
        <label className="registration__label">
          <span>password</span>
          <input
            className="registration__input"
            type="password"
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            placeholder="create password"
          />
        </label>
        <label className="registration__label">
          <span>confirm password</span>
          <input
            className={`registration__input ${passwordMismatch ? "registration__input--error" : ""}`}
            type="password"
            value={confirmPassword}
            onChange={(e) => setConfirmPassword(e.target.value)}
            placeholder="repeat password"
          />
          {passwordMismatch && (
            <span className="registration__error">passwords don't match</span>
          )}
        </label>
      </div>
      <button
        className="registration__submit"
        type="submit"
        disabled={!canSubmit}
      >
        sign up
      </button>
    </form>
  );
}

export default SignupForm;
