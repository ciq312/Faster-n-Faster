import { useState } from "react";

function LoginForm({ onSubmit, loading }) {
  const [login, setLogin] = useState("");
  const [password, setPassword] = useState("");

  const canSubmit = login.trim() && password.trim();

  const handleSubmit = async (e) => {
    e.preventDefault();
    if (!canSubmit || loading) return;
    await onSubmit({ login, password });
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
            placeholder="username or email"
            autoFocus
          />
        </label>
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
      </div>
      <button
        className="registration__submit"
        type="submit"
        disabled={!canSubmit || loading}
      >
        {loading ? "loading..." : "log in"}
      </button>
    </form>
  );
}

export default LoginForm;
