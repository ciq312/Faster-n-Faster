import type { BannerVariant } from "../../banner/bannerSlice";
import "./Banner.css";

type BannerProps = {
  variant: BannerVariant;
  message: string;
  fading: boolean;
};

function Banner({ variant, message, fading }: BannerProps) {
  if (!message) return null;

  return (
    <div
      className={`${variant}-banner ${fading ? `${variant}-banner--fading` : ""}`}
    >
      {message}
    </div>
  );
}

export default Banner;
