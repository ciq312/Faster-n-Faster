import { useState, useEffect } from "react";
import "./ErrorBanner.css";

function ErrorBanner({ message, duration = 4000, onDismiss }) {
  const [fading, setFading] = useState(false);

  useEffect(() => {
    if (!message) return;
    setFading(false);

    const fadeTimer = setTimeout(() => setFading(true), duration - 400);
    const removeTimer = setTimeout(() => onDismiss?.(), duration);

    return () => {
      clearTimeout(fadeTimer);
      clearTimeout(removeTimer);
    };
  }, [message, duration, onDismiss]);

  if (!message) return null;

  return (
    <div className={`error-banner ${fading ? "error-banner--fading" : ""}`}>
      {message}
    </div>
  );
}

export default ErrorBanner;
