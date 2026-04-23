import { useEffect, useState } from "react";
import { Link, useSearchParams } from "react-router-dom";
import "./VerifyEmail.css";

function VerifyEmail() {
  const [params] = useSearchParams();
  const token = params.get("token");
  const [status, setStatus] = useState("verifying");

  useEffect(() => {
    if (!token) {
      setStatus("missing");
      return;
    }

    const verify = async () => {
      try {
        const res = await fetch("/api/auth/verify-email", {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({ token }),
        });
        setStatus(res.ok ? "success" : "failed");
      } catch {
        setStatus("failed");
      }
    };
    const verifyDelayed = async () => {
      await new Promise((res) => setTimeout(res, 1000));
      verify();
    };
    verifyDelayed();
  }, [token]);

  return (
    <div className="verify-email">
      <div className="verify-email__header">
        <h1 className="verify-email__logo">verify email</h1>
      </div>

      <div className="verify-email__card">
        {status === "verifying" && (
          <p className="verify-email__body">verifying…</p>
        )}
        {status === "missing" && (
          <p className="verify-email__body">no token provided.</p>
        )}
        {status === "failed" && (
          <>
            <p className="verify-email__body">
              invalid or expired link. request a new one from the sign-up page.
            </p>
            <Link to="/" className="verify-email__button">
              back to sign up
            </Link>
          </>
        )}
        {status === "success" && (
          <>
            <p className="verify-email__body">
              your email is verified. you can now log in.
            </p>
            <Link to="/" className="verify-email__button">
              go to login
            </Link>
          </>
        )}
      </div>
    </div>
  );
}

export default VerifyEmail;
