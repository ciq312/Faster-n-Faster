import { useState, type FormEvent } from "react";
import { Link } from "react-router-dom";
import type { LoginUserRequest } from "../../../../types";

type LoginFormProps = {
  onSubmit: (credentials: LoginUserRequest) => Promise<void>;
  loading: boolean;
};

function LoginForm({ onSubmit, loading }: LoginFormProps) {
  const [login, setLogin] = useState("");
  const [password, setPassword] = useState("");

  const canSubmit = login.trim() && password.trim();

  const handleSubmit = async (e: FormEvent<HTMLFormElement>) => {
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
      <Link to="/forgot-password" className="registration__forgot">
        forgot password?
      </Link>
    </form>
  );
}

export default LoginForm;
