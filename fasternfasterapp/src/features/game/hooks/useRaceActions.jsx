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
      subscribe("RaceWithdrawn", () => {
        showMessage("You were removed from this race for suspicious input");
      }),
    ];
    return () => cleanups.forEach((fn) => fn());
  }, [isConnected, subscribe, showMessage]);

  const rawSendProgress = useCallback(
    async ({ index, mistakes, typed }) => {
      await invoke("UpdateRaceState", index, mistakes, typed);
    },
    [invoke],
  );

  const { throttled: sendProgress, flush: flushProgress } =
    useThrottledCallback(rawSendProgress, PROGRESS_THROTTLE_MS);

  const startRace = useCallback(async () => {
    await invoke("StartRace");
  }, [invoke]);

  const refreshPassage = useCallback(async () => {
    await invoke("RefreshPassage");
  }, [invoke]);

  return {
    startRace,
    sendProgress,
    flushProgress,
    refreshPassage,
  };
}
