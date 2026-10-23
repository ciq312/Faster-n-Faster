import { Link, useSearchParams } from "react-router-dom";
import { useResendVerificationMutation } from "../../features/auth/authApi";
import { errorMessage } from "../../shared/api/errorMessage";
import { useError } from "../../shared/components/BannerProvider";
import { useCooldown } from "../../shared/hooks/useCooldown";
import "./CheckYourEmail.css";

const COOLDOWN_SECONDS = 15;

function CheckYourEmail() {
  const [params] = useSearchParams();
  const email = params.get("email") ?? "";
  const { showError } = useError();
  const { cooldown, start } = useCooldown(COOLDOWN_SECONDS, {
    startActive: true,
  });
  const [resendVerification, { isLoading }] = useResendVerificationMutation();

  const handleResend = async () => {
    if (!email || isLoading || cooldown > 0) return;
    const result = await resendVerification({ email });
    if (result.error) showError(errorMessage(result.error));
    else start();
  };

  return (
    <div className="check-email">
      <div className="check-email__header">
        <h1 className="check-email__logo">check your email</h1>
        <p className="check-email__tagline">one more step</p>
      </div>

      <div className="check-email__card">
        <p className="check-email__body">
          we sent a verification link to{" "}
          <strong className="check-email__address">
            {email || "your inbox"}
          </strong>
          . click it to activate your account, then log in.
        </p>

        <button
          type="button"
          className="check-email__resend"
          onClick={handleResend}
          disabled={!email || isLoading || cooldown > 0}
        >
          {isLoading
            ? "sending..."
            : cooldown > 0
              ? `resend in ${cooldown}s`
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
