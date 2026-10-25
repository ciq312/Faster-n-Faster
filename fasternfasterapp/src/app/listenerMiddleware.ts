import {
  addListener,
  createListenerMiddleware,
  isAnyOf,
  isRejectedWithValue,
  type ListenerMiddlewareInstance,
  type ThunkDispatch,
  type UnknownAction,
} from "@reduxjs/toolkit";
import { authApi } from "../features/auth/authApi";
import { hubEvents } from "../features/connection/hubEvents";
import {
  bannerHubCalls,
  connectHub,
  disconnectHub,
} from "../features/connection/hubThunks";
import { notificationFor } from "../features/connection/notifications";
import { showError, showMessage } from "../shared/banner/bannerSlice";
import type { AppDispatch, RootState, ThunkExtra } from "./store";

export function createAppListenerMiddleware(
  extra: ThunkExtra,
): ListenerMiddlewareInstance<
  unknown,
  ThunkDispatch<unknown, unknown, UnknownAction>,
  ThunkExtra
> {
  const instance = createListenerMiddleware({ extra });
  const startListening = instance.startListening.withTypes<
    RootState,
    AppDispatch,
    ThunkExtra
  >();

  startListening({
    predicate: (action) => notificationFor(action) !== null,
    effect: (action, { dispatch }) => {
      const notification = notificationFor(action);
      if (!notification) return;
      dispatch(
        notification.variant === "error"
          ? showError(notification.message)
          : showMessage(notification.message),
      );
    },
  });

  startListening({
    matcher: isAnyOf(
      hubEvents.banned,
      hubEvents.suspended,
      hubEvents.anotherSessionStarted,
    ),
    effect: async (_, { extra }) => {
      await extra.hub.stop();
    },
  });

  const isBannerHubCallRejected = isRejectedWithValue(...bannerHubCalls);
  startListening({
    matcher: isBannerHubCallRejected,
    effect: (action, { dispatch }) => {
      dispatch(showError(action.payload));
    },
  });

  startListening({
    actionCreator: connectHub.rejected,
    effect: (_, { dispatch }) => {
      dispatch(showError("Can't connect to the server"));
    },
  });

  startListening({
    matcher: authApi.endpoints.logout.matchFulfilled,
    effect: (_, { dispatch }) => {
      dispatch(disconnectHub());
    },
  });

  return instance;
}

export const addAppListener = addListener.withTypes<
  RootState,
  AppDispatch,
  ThunkExtra
>();
