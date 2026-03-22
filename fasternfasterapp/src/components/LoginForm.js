import { useState } from "react";

function LoginForm({ onSubmit }) {
  const [login, setLogin] = useState("");
  const [password, setPassword] = useState("");

  const canSubmit = login.trim() && password.trim();

  const handleSubmit = async (e) => {
    e.preventDefault();
    if (!canSubmit) return;
    let errors = await onSubmit({ login, password });
    if (errors) handleServerErrors(errors);
  };

  const handleServerErrors = (error) => {};
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
            placeholder="username or email"
            autoFocus
          />
        </label>
        <div className="loginError"></div>
        <label className="registration__label">
          <span>password</span>
          <input
            className="registration__input"
            type="password"
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            placeholder="enter password"
          />
        </label>
        <div className="passwordError"></div>
      </div>
      <button
        className="registration__submit"
        type="submit"
        disabled={!canSubmit}
      >
        log in
      </button>
    </form>
  );
}

export default LoginForm;
