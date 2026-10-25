import { useCallback, useEffect } from "react";
import { useAppDispatch } from "../../../app/hooks";
import { showMessage } from "../../../shared/banner/bannerSlice";
import { useConnection } from "../../connection/ConnectionProvider";
import { useThrottledCallback } from "./useThrottledCallback";

const PROGRESS_THROTTLE_MS = 100;

export function useRaceActions() {
  const dispatch = useAppDispatch();
  const { invoke, invokeOrShowError, subscribe, isConnected } = useConnection();

  useEffect(() => {
    const cleanups = [
      subscribe("PlayerFinished", (data) => {
        dispatch(
          showMessage(
            `${data.nick} finished ${data.finishPosition} with wpm:${Math.trunc(Number(data.wpm))}`,
          ),
        );
      }),
      subscribe("RaceWithdrawn", () => {
        dispatch(
          showMessage("You were removed from this race for suspicious input"),
        );
      }),
    ];
    return () => cleanups.forEach((fn) => fn());
  }, [isConnected, subscribe, dispatch]);

  const rawSendProgress = useCallback(
    async ({ index, mistakes, typed }) => {
      await invoke("UpdateRaceState", index, mistakes, typed);
    },
    [invoke],
  );

  const { throttled: sendProgress, flush: flushProgress } =
    useThrottledCallback(rawSendProgress, PROGRESS_THROTTLE_MS);

  const startRace = useCallback(async () => {
    await invokeOrShowError("StartRace");
  }, [invokeOrShowError]);

  const refreshPassage = useCallback(async () => {
    await invokeOrShowError("RefreshPassage");
  }, [invokeOrShowError]);

  return {
    startRace,
    sendProgress,
    flushProgress,
    refreshPassage,
  };
}
