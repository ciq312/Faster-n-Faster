import "./Banner.css";

function Banner({ variant, message, fading }) {
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
