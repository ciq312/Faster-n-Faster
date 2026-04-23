import { Link, useSearchParams } from "react-router-dom";
import { useResendVerification } from "../../features/auth/hooks/useResendVerification";
import "./CheckYourEmail.css";

function CheckYourEmail() {
  const [params] = useSearchParams();
  const email = params.get("email") ?? "";
  const resend = useResendVerification();

  return (
    <div className="check-email">
      <div className="check-email__header">
        <h1 className="check-email__logo">check your email</h1>
        <p className="check-email__tagline">one more step</p>
      </div>

      <div className="check-email__card">
        <p className="check-email__body">
          we sent a verification link to{" "}
          <strong className="check-email__address">{email || "your inbox"}</strong>.
          click it to activate your account, then log in.
        </p>

        <button
          type="button"
          className="check-email__resend"
          onClick={() => resend.execute(email)}
          disabled={!email || resend.loading || resend.cooldown > 0}
        >
          {resend.loading
            ? "sending..."
            : resend.cooldown > 0
              ? `resend in ${resend.cooldown}s`
              : "resend link"}
        </button>

        <Link to="/" className="check-email__back">
          wrong address? register again
        </Link>
      </div>
    </div>
  );
}

export default CheckYourEmail;
