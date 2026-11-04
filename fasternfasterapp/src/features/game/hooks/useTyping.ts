import {
  useCallback,
  useEffect,
  useRef,
  useState,
  type ChangeEvent,
  type RefObject,
} from "react";
import type { KeystrokeEventDTO } from "../../../../generated/signalr/FasterNFaster.Api.Presentation.Web.Realtime";
import { useAppDispatch, useAppSelector } from "../../../app/hooks";
import { selectIsConnected } from "../../connection/connectionSlice";
import { getRaceProgress } from "../../connection/hubThunks";

const MAX_OVERFLOW = 6;

export type TypingProgress = { keystroke: KeystrokeEventDTO; final: boolean };

type TypingOptions = {
  passage: string;
  disabled: boolean;
  onProgress: (progress: TypingProgress) => void;
};

export function useTyping({ passage, disabled, onProgress }: TypingOptions): {
  typed: string;
  inputRef: RefObject<HTMLInputElement | null>;
  handleTyping: (e: ChangeEvent<HTMLInputElement>) => void;
  focusInput: () => void;
  nextSepIndex: number;
} {
  const [typed, setTyped] = useState("");
  const dispatch = useAppDispatch();
  const mistakesRef = useRef(0);
  const inputRef = useRef<HTMLInputElement>(null);
  const lastValidIndexRef = useRef(-1);
  const nextSep = useRef(-1);
  const needResyncRef = useRef(true);
  const isConnected = useAppSelector(selectIsConnected);

  const sendProgress = useCallback(
    (keystroke: KeystrokeEventDTO) => {
      onProgress({
        keystroke,
        final: lastValidIndexRef.current === passage.length - 1,
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
    const resync = async () => {
      const result = await dispatch(getRaceProgress());
      if (!getRaceProgress.fulfilled.match(result)) return;
      needResyncRef.current = false;
      const progress = result.payload;
      if (!progress) return;
      setTyped(
        passage.substring(0, progress.lastValidIndex + 1) + progress.wrongTail,
      );
      lastValidIndexRef.current = progress.lastValidIndex;
      mistakesRef.current = progress.mistakes;
      nextSep.current = passage.indexOf(" ", progress.lastValidIndex + 1);
    };
    resync();
  }, [isConnected, passage, dispatch]);

  const handleCharTyped = useCallback(
    (value: string): string => {
      const newChar = value[value.length - 1];
      const nextChar = passage[value.length - 1];
      const isNewCharCorrect = newChar === nextChar;

      if (isNewCharCorrect) {
        setTyped(value);

        const isPreviousCharCorrect =
          lastValidIndexRef.current + 1 === value.length - 1;
        if (isPreviousCharCorrect) {
          lastValidIndexRef.current++;
          const isLastChar = lastValidIndexRef.current === passage.length - 1;
          if (isLastChar) inputRef.current?.blur();
        }
      } else {
        setTyped(value);
        mistakesRef.current++;
      }

      return newChar;
    },
    [passage],
  );

  const handleBackspace = useCallback((value: string): string => {
    lastValidIndexRef.current = Math.min(
      lastValidIndexRef.current,
      value.length - 1,
    );
    setTyped(value);

    return "Backspace";
  }, []);

  const handleTyping = useCallback(
    (e: ChangeEvent<HTMLInputElement>) => {
      // Until the server's progress is restored, the server would apply the keystroke at a different position.
      if (disabled || needResyncRef.current) return;
      const value = e.target.value;

      const isMaxOverflow =
        value.length > typed.length &&
        value.length - 1 - lastValidIndexRef.current > MAX_OVERFLOW;
      if (isMaxOverflow) return;

      const isPastPassageEnd = value.length > passage.length;
      if (isPastPassageEnd) return;

      const isNewCharTyped = value.length - 1 === typed.length;
      const isCharDeleted = value.length + 1 === typed.length;

      if (isNewCharTyped) {
        const newChar = value[value.length - 1];
        const isPreviousBeforeSpaceInCorrect =
          newChar === ` ` && lastValidIndexRef.current !== value.length - 1 - 1;

        if (isPreviousBeforeSpaceInCorrect) return;

        sendProgress({ key: handleCharTyped(value) });
      } else if (isCharDeleted) {
        sendProgress({ key: handleBackspace(value) });
      }

      nextSep.current = passage.indexOf(" ", lastValidIndexRef.current + 1);
    },
    [
      disabled,
      typed.length,
      passage,
      handleBackspace,
      handleCharTyped,
      sendProgress,
    ],
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
