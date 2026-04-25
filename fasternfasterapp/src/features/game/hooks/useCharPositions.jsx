import { useState, useRef, useCallback, useEffect, useLayoutEffect } from "react";

export function useCharPositions(passage, typedLength) {
  const charsRef = useRef([]);
  const containerRef = useRef(null);
  const [charPositions, setCharPositions] = useState([]);

  const measure = useCallback(() => {
    if (!containerRef.current) return;
    const containerRect = containerRef.current.getBoundingClientRect();
    const positions = charsRef.current.map((el) => {
      if (!el) return { left: 0, top: 0 };
      const rect = el.getBoundingClientRect();
      return {
        left: rect.left - containerRect.left,
        top: rect.top - containerRect.top,
      };
    });
    setCharPositions(positions);
  }, []);

  useLayoutEffect(() => {
    measure();
  }, [passage, typedLength, measure]);

  useEffect(() => {
    window.addEventListener("resize", measure);
    return () => window.removeEventListener("resize", measure);
  }, [measure]);

  const caretPos = useCallback(
    (index) => {
      if (!charPositions.length) return { left: 0, top: 0 };
      if (index < charPositions.length) return charPositions[index];
      const last = charPositions[charPositions.length - 1];
      const lastEl = charsRef.current[charsRef.current.length - 1];
      return { left: last.left + (lastEl?.offsetWidth || 10), top: last.top };
    },
    [charPositions],
  );

  return { charsRef, containerRef, caretPos };
}
