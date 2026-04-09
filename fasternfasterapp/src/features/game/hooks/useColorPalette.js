import { useState, useEffect, useRef } from "react";

export function useColorPalette() {
  const [show, setShow] = useState(false);
  const panelRef = useRef(null);

  useEffect(() => {
    if (!show) return;
    const handleClick = (e) => {
      if (panelRef.current && !panelRef.current.contains(e.target))
        setShow(false);
    };
    document.addEventListener("mousedown", handleClick);
    return () => document.removeEventListener("mousedown", handleClick);
  }, [show]);

  const toggle = (canOpen) => {
    setShow(show ? false : canOpen);
  };

  const close = () => setShow(false);

  return { showColors: show, panelRef, toggle, close };
}
