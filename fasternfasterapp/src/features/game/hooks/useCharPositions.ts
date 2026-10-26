import {
  useState,
  useRef,
  useCallback,
  useEffect,
  useLayoutEffect,
  type RefObject,
} from "react";

type CharPosition = { left: number; top: number };

export function useCharPositions(
  passage: string,
  typedLength: number,
): {
  charsRef: RefObject<(HTMLSpanElement | null)[]>;
  containerRef: RefObject<HTMLDivElement | null>;
  caretPos: (index: number) => CharPosition;
} {
  const charsRef = useRef<(HTMLSpanElement | null)[]>([]);
  const containerRef = useRef<HTMLDivElement>(null);
  const [charPositions, setCharPositions] = useState<CharPosition[]>([]);

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
    (index: number): CharPosition => {
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
