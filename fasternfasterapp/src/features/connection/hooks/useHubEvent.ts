import type { PayloadActionCreator } from "@reduxjs/toolkit";
import { useEffect, useEffectEvent } from "react";
import { addAppListener } from "../../../app/listenerMiddleware";
import { useAppDispatch } from "../../../app/hooks";

export function useHubEvent<P>(
  actionCreator: PayloadActionCreator<P>,
  handler: (payload: P) => void,
): void {
  const dispatch = useAppDispatch();
  const onEvent = useEffectEvent(handler);

  useEffect(() => {
    const unsubscribe = dispatch(
      addAppListener({
        actionCreator,
        effect: (action) => onEvent(action.payload),
      }),
    );
    return unsubscribe;
  }, [dispatch, actionCreator]);
}
