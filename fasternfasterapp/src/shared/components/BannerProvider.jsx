import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useRef,
  useState,
} from "react";
import Banner from "./Banner/Banner";
const BannerContext = createContext();

function BannerProvider({ children }) {
  const bannerRef = useRef(null);
  const bannerPromiseRef = useRef(null);
  const [banner, setBanner] = useState(null);
  const [fading, setFading] = useState(false);
  const timers = useRef({ fade: null, remove: null });

  const clearTimers = () => {
    clearTimeout(timers.current.fade);
    clearTimeout(timers.current.remove);
  };

  const closeBanner = useCallback(() => {
    return new Promise((resolve) => {
      if (!bannerRef.current) {
        resolve();
        return;
      }
      clearTimers();
      setFading(true);
      timers.current.remove = setTimeout(() => {
        bannerRef.current = null;
        setBanner(null);
        setFading(false);
        resolve();
      }, 400);
    });
  }, []);

  const showBanner = useCallback(
    async ({ variant, message, duration_MS = 4000 }) => {
      if (bannerRef.current) {
        await bannerPromiseRef.current;
      }
      clearTimers();
      const next = { variant, message, duration_MS };
      bannerRef.current = next;
      setFading(false);
      setBanner(next);
      timers.current.fade = setTimeout(() => {
        setFading(true);
      }, duration_MS - 400);
      bannerPromiseRef.current = new Promise(resolve => {timers.current.remove = setTimeout(() => {
        bannerRef.current = null;
        setBanner(null);
        setFading(false);
        resolve();
      }, duration_MS);
    });
    },
    [closeBanner],
  );

  useEffect(() => () => clearTimers(), []);

  const value = useMemo(
    () => ({ showBanner, closeBanner }),
    [showBanner, closeBanner],
  );

  return (
    <BannerContext.Provider value={value}>
      {banner && (
        <Banner
          variant={banner.variant}
          message={banner.message}
          fading={fading}
        />
      )}
      {children}
    </BannerContext.Provider>
  );
}

export function useBanner() {
  return useContext(BannerContext);
}

export function useError() {
  const { showBanner, closeBanner } = useContext(BannerContext);

  const showError = useCallback(
    (message) => showBanner({ variant: "error", message }),
    [showBanner],
  );
  return { showError, closeBanner };
}

export function useBannerMessage() {
  const { showBanner, closeBanner } = useContext(BannerContext);

  const showMessage = useCallback(
    (message) => showBanner({ variant: "message", message }),
    [showBanner],
  );

  return { showMessage, closeBanner };
}

export default BannerProvider;
