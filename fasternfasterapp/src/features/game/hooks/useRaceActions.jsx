import { useCallback, useEffect } from "react";
import { useBannerMessage } from "../../../shared/components/BannerProvider";
import { useConnection } from "../../connection/ConnectionProvider";
import { useThrottledCallback } from "./useThrottledCallback";

const PROGRESS_THROTTLE_MS = 100;

export function useRaceActions() {
  const { invoke, subscribe, isConnected } = useConnection();
  const { showMessage } = useBannerMessage();

  useEffect(() => {
    const cleanups = [
      subscribe("PlayerFinished", (data) => {
        showMessage(
          `${data.nick} finished ${data.finishPosition} with wpm:${Math.trunc(Number(data.wpm))}`,
        );
      }),
    ];
    return () => cleanups.forEach((fn) => fn());
  }, [isConnected]);

  const rawSendProgress = useCallback(async ({ index, mistakes, typed }) => {
    await invoke("UpdateRaceState", index, mistakes, typed);
  }, []);

  const { throttled: sendProgress, flush: flushProgress } =
    useThrottledCallback(rawSendProgress, PROGRESS_THROTTLE_MS);

  const startRace = useCallback(async () => {
    await invoke("StartRace");
  }, []);

  const changeGameMode = useCallback(async (mode) => {
    await invoke("ChangeGameMode", mode);
  }, []);

  const changeWordCount = useCallback(async (count) => {
    await invoke("ChangeWordCount", count);
  }, []);

  const changeTimerDuration = useCallback(async (duration) => {
    await invoke("ChangeTimerDuration", duration);
  }, []);

  const refreshPassage = useCallback(async () => {
    await invoke("RefreshPassage");
  }, []);

  return {
    startRace,
    sendProgress,
    flushProgress,
    refreshPassage,
    changeGameMode,
    changeTimerDuration,
    changeWordCount,
  };
}
