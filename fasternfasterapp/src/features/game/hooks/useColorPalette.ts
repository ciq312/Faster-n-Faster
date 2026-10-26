import { useState, useEffect, useRef, type RefObject } from "react";

export function useColorPalette(): {
  showColors: boolean;
  panelRef: RefObject<HTMLDivElement | null>;
  toggle: (canOpen: boolean) => void;
  close: () => void;
} {
  const [show, setShow] = useState(false);
  const panelRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (!show) return;
    const handleClick = (e: MouseEvent) => {
      if (
        panelRef.current &&
        !panelRef.current.contains(e.target as Node | null)
      ) {
        setShow(false);
      }
    };
    document.addEventListener("mousedown", handleClick);
    return () => document.removeEventListener("mousedown", handleClick);
  }, [show]);

  const toggle = (canOpen: boolean) => {
    setShow(show ? false : canOpen);
  };

  const close = () => setShow(false);

  return { showColors: show, panelRef, toggle, close };
}
