import { useCallback, useEffect, useRef } from "react";

export function useThrottledCallback(fn, intervalMs = 100) {
  const fnRef = useRef(fn);
  fnRef.current = fn;

  const pendingArgsRef = useRef(null);
  const lastInvokeAtRef = useRef(0);
  const timerRef = useRef(null);

  const invoke = useCallback((args) => {
    pendingArgsRef.current = null;
    lastInvokeAtRef.current = Date.now();
    fnRef.current(args);
  }, []);

  const onTimerFire = useCallback(() => {
    timerRef.current = null;
    if (pendingArgsRef.current !== null) invoke(pendingArgsRef.current);
  }, [invoke]);

  const throttled = useCallback(
    (args) => {
      const elapsed = Date.now() - lastInvokeAtRef.current;
      if (elapsed >= intervalMs) {
        invoke(args);
        return;
      }
      pendingArgsRef.current = args;
      if (!timerRef.current) {
        timerRef.current = setTimeout(onTimerFire, intervalMs - elapsed);
      }
    },
    [invoke, onTimerFire, intervalMs],
  );

  const flush = useCallback(() => {
    if (timerRef.current) {
      clearTimeout(timerRef.current);
      timerRef.current = null;
    }
    if (pendingArgsRef.current !== null) invoke(pendingArgsRef.current);
  }, [invoke]);

  useEffect(
    () => () => {
      if (timerRef.current) clearTimeout(timerRef.current);
    },
    [],
  );

  return { throttled, flush };
}
