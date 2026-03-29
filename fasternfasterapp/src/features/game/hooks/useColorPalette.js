import { useState, useEffect, useRef } from "react";

export function useColorPalette() {
  const [showColors, setShowColors] = useState(false);
  const panelRef = useRef(null);

  useEffect(() => {
    if (!showColors) return;
    const handleClick = (e) => {
      if (panelRef.current && !panelRef.current.contains(e.target))
        setShowColors(false);
    };
    document.addEventListener("mousedown", handleClick);
    return () => document.removeEventListener("mousedown", handleClick);
  }, [showColors]);

  const toggle = (canOpen) => {
    setShowColors(showColors ? false : canOpen);
  };

  const close = () => setShowColors(false);

  return { showColors, panelRef, toggle, close };
}
