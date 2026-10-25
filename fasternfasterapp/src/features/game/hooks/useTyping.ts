import {
  useCallback,
  useEffect,
  useRef,
  useState,
  type RefObject,
} from "react";
import { useAppSelector } from "../../../app/hooks";
import { selectIsConnected } from "../../connection/connectionSlice";

const MAX_OVERFLOW = 6;

export type TypingProgress = {
  index: number;
  mistakes: number;
  typed: string;
  final: boolean;
};

type TypingOptions = {
  passage: string;
  disabled: boolean;
  onProgress?: (progress: TypingProgress) => void;
  selfTyped?: string;
  selfCorrectIndex?: number;
  selfMistakes?: number;
};

export function useTyping({
  passage,
  disabled,
  onProgress,
  selfTyped,
  selfCorrectIndex,
  selfMistakes,
}: TypingOptions): {
  typed: string;
  inputRef: RefObject<HTMLInputElement | null>;
  handleTyping: (e: { target: { value: string } }) => void;
  focusInput: () => void;
  nextSepIndex: number;
} {
  const [typed, setTyped] = useState("");
  const mistakesRef = useRef(0);
  const inputRef = useRef<HTMLInputElement>(null);
  const lastCorrectIndexRef = useRef(-1);
  const nextSep = useRef(-1);
  const needResyncRef = useRef(true);
  const hasSyncedRef = useRef(false);
  const isConnected = useAppSelector(selectIsConnected);

  const sendProgress = useCallback(
    (value: string) => {
      // Sending before the first RaceState would overwrite the server's progress with the reloaded, empty text.
      if (needResyncRef.current) return;
      onProgress?.({
        index: lastCorrectIndexRef.current,
        mistakes: mistakesRef.current,
        typed: value,
        final: lastCorrectIndexRef.current === passage.length - 1,
      });
    },
    [onProgress, passage],
  );

  useEffect(() => {
    if (!isConnected) {
      needResyncRef.current = true;
    }
  }, [isConnected]);
  useEffect(() => {
    if (!isConnected || !needResyncRef.current) return;
    if (selfTyped === undefined || selfCorrectIndex === undefined) return;
    needResyncRef.current = false;
    const isReconnect = hasSyncedRef.current;
    hasSyncedRef.current = true;
    // A longer server text wins only after a reload; after a reconnect the local text is fresher.
    if (isReconnect || selfTyped.length <= typed.length) {
      if (typed) sendProgress(typed);
      return;
    }
    setTyped(selfTyped);
    lastCorrectIndexRef.current = selfCorrectIndex;
    nextSep.current = passage.indexOf(" ", selfCorrectIndex + 1);
    mistakesRef.current = selfMistakes ?? 0;
  }, [
    isConnected,
    selfTyped,
    passage,
    selfCorrectIndex,
    selfMistakes,
    typed,
    sendProgress,
  ]);
  const handleTyping = useCallback(
    (e: { target: { value: string } }) => {
      if (disabled) return;
      const value = e.target.value;

      const isMaxOverflow =
        value.length > typed.length &&
        value.length - 1 - lastCorrectIndexRef.current > MAX_OVERFLOW;
      if (isMaxOverflow) return;

      const isNewCharTyped = value.length - 1 === typed.length;
      const isCharDeleted = value.length + 1 === typed.length;

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
            const isLastChar =
              lastCorrectIndexRef.current === passage.length - 1;
            if (isLastChar) inputRef.current?.blur();
          }
        } else {
          setTyped(value);
          mistakesRef.current++;
        }
      } else if (isCharDeleted) {
        lastCorrectIndexRef.current = Math.min(
          lastCorrectIndexRef.current,
          value.length - 1,
        );
        setTyped(value);
      }

      nextSep.current = passage.indexOf(" ", lastCorrectIndexRef.current + 1);

      sendProgress(value);
    },
    [typed.length, passage, disabled, sendProgress],
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
    nextSepIndex: nextSep.current,
  };
}
