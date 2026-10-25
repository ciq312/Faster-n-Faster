import { useCallback } from "react";
import { useAppDispatch } from "../../../app/hooks";
import {
  refreshPassage as refreshPassageThunk,
  startRace as startRaceThunk,
  updateRaceState,
} from "../../connection/hubThunks";
import { useThrottledCallback } from "./useThrottledCallback";

const PROGRESS_THROTTLE_MS = 100;

export function useRaceActions() {
  const dispatch = useAppDispatch();

  const rawSendProgress = useCallback(
    ({ index, mistakes, typed }) =>
      dispatch(updateRaceState({ index, mistakes, typed })),
    [dispatch],
  );

  const { throttled: sendProgress, flush: flushProgress } =
    useThrottledCallback(rawSendProgress, PROGRESS_THROTTLE_MS);

  const startRace = useCallback(() => dispatch(startRaceThunk()), [dispatch]);

  const refreshPassage = useCallback(
    () => dispatch(refreshPassageThunk()),
    [dispatch],
  );

  return {
    startRace,
    sendProgress,
    flushProgress,
    refreshPassage,
  };
}
