import { useState, useRef, useCallback, useEffect } from "react";

export function useTyping({ passage, disabled, onProgress }) {
  const [typed, setTyped] = useState("");
  const mistakesRef = useRef(0);
  const inputRef = useRef(null);
  const lastCorrectIndexRef = useRef(-1);

  const handleTyping = useCallback(
    (e) => {
      if (disabled) return;
      const value = e.target.value;
      console.log("Typed value:", value);
      if (value.length > typed.length + 1) return;
      if (value.length > passage.length) return;
      if (value.length < typed.length - 1) return;

      if (value.length > typed.length) {
        const newChar = value[value.length - 1];
        if (newChar === passage[value.length - 1]) {
          setTyped(value);
          if (lastCorrectIndexRef.current + 1 === value.length - 1) {
            lastCorrectIndexRef.current = lastCorrectIndexRef.current + 1;
          }
        } else {
          setTyped(value);
          mistakesRef.current++;
        }
      } else if (value.length < typed.length) {
        setTyped(value);
      }

      onProgress?.({
        index: lastCorrectIndexRef.current,
        mistakes: mistakesRef.current,
      });
    },
    [typed.length, passage, disabled, onProgress],
  );

  useEffect(() => {
    if (!disabled) inputRef.current?.focus();
  }, [disabled]);

  const focusInput = useCallback(
    () => !disabled && inputRef.current?.focus(),
    [disabled],
  );

  return { typed, inputRef, handleTyping, focusInput };
}
