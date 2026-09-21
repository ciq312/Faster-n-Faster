import { useCallback, useEffect, useRef, useState } from "react";
import { useConnection } from "../../connection/ConnectionProvider";

const MAX_OVERFLOW = 6;

export function useTyping({
  passage,
  disabled,
  onProgress,
  selfTyped,
  selfCorrectIndex,
  selfMistakes,
}) {
  const [typed, setTyped] = useState("");
  const mistakesRef = useRef(0);
  const inputRef = useRef(null);
  const lastCorrectIndexRef = useRef(-1);
  const nextSep = useRef(-1);
  const needResyncRef = useRef(true);
  const { isConnected } = useConnection();

  useEffect(() => {
    if (!isConnected) {
      needResyncRef.current = true;
    }
  }, [isConnected]);
  useEffect(() => {
    if (!needResyncRef.current) return;
    if (selfTyped === undefined || selfCorrectIndex === undefined) return;
    setTyped(selfTyped);
    lastCorrectIndexRef.current = selfCorrectIndex;
    nextSep.current = passage.indexOf(" ", selfCorrectIndex + 1);
    mistakesRef.current = selfMistakes ?? 0;
    needResyncRef.current = false;
  }, [selfTyped, passage, selfCorrectIndex, selfMistakes]);
  const handleTyping = useCallback(
    (e) => {
      if (disabled) return;
      const value = e.target.value;

      const isMaxOverflow =
        value.length > typed.length &&
        value.length - 1 - lastCorrectIndexRef.current > MAX_OVERFLOW;
      if (isMaxOverflow) return;

      const isNewCharTyped = value.length - 1 === typed.length;
      const IsCharDeleted = value.length + 1 === typed.length;

      if (isNewCharTyped) {
        const newChar = value[value.length - 1];
        const isPreviousBeforeSpaceInCorrect =
          newChar === ` ` &&
          lastCorrectIndexRef.current !== value.length - 1 - 1;

        if (isPreviousBeforeSpaceInCorrect) return;

        const isNewCharCorrect = newChar === passage[value.length - 1];
        if (isNewCharCorrect) {
          setTyped(value);
          const isPreviousCharCorrect =
            lastCorrectIndexRef.current + 1 === value.length - 1;
          if (isPreviousCharCorrect) {
            lastCorrectIndexRef.current = lastCorrectIndexRef.current + 1;
            const IsLastChar =
              lastCorrectIndexRef.current === passage.length - 1;
            if (IsLastChar) inputRef.current.blur();
          }
        } else {
          const handleMistake = () => {
            setTyped(value);
            mistakesRef.current++;
          };
          handleMistake();
        }
      } else if (IsCharDeleted) {
        lastCorrectIndexRef.current = Math.min(
          lastCorrectIndexRef.current,
          value.length - 1,
        );
        setTyped(value);
      }

      nextSep.current = passage.indexOf(" ", lastCorrectIndexRef.current + 1);

      const isFinal = lastCorrectIndexRef.current === passage.length - 1;
      onProgress?.({
        index: lastCorrectIndexRef.current,
        mistakes: mistakesRef.current,
        typed: value,
        final: isFinal,
      });
    },
    [typed.length, passage, disabled, onProgress],
  );

  useEffect(() => {
    if (!disabled) {
      inputRef.current?.focus();
    }
  }, [disabled]);

  const focusInput = useCallback(
    () => !disabled && inputRef.current?.focus(),
    [disabled],
  );

  return {
    typed,
    inputRef,
    handleTyping,
    focusInput,
    lastCorrectIndex: lastCorrectIndexRef.current,
    nextSepIndex: nextSep.current,
  };
}
