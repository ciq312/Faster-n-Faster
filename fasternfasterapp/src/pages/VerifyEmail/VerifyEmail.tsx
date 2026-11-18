import { useEffect } from "react";
import { Link, useSearchParams } from "react-router-dom";
import { useVerifyEmailMutation } from "../../features/auth/authApi";
import "./VerifyEmail.css";

function verificationStatus(
  token: string | null,
  { isSuccess, isError }: { isSuccess: boolean; isError: boolean },
) {
  if (!token) return "missing";
  if (isSuccess) return "success";
  if (isError) return "failed";
  return "verifying";
}

function VerifyEmail() {
  const [params] = useSearchParams();
  const token = params.get("token");
  const [verifyEmail, verification] = useVerifyEmailMutation();
  const status = verificationStatus(token, verification);

  useEffect(() => {
    if (!token) return;
    const id = setTimeout(() => verifyEmail({ token }), 1000);
    return () => clearTimeout(id);
  }, [token, verifyEmail]);

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
