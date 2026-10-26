import { combineSlices, configureStore } from "@reduxjs/toolkit";
import { API_BASE } from "../config";
import { connectionSlice } from "../features/connection/connectionSlice";
import type { GameHub } from "../features/connection/gameHub";
import { createSignalRGameHub } from "../features/connection/signalRGameHub";
import { raceSlice } from "../features/game/raceSlice";
import { lobbySlice } from "../features/lobbies/lobbySlice";
import { api } from "../shared/api/api";
import { bannerSlice } from "../shared/banner/bannerSlice";
import { createAppListenerMiddleware } from "./listenerMiddleware";

const rootReducer = combineSlices(
  api,
  bannerSlice,
  connectionSlice,
  lobbySlice,
  raceSlice,
);

export type ThunkExtra = { hub: GameHub };

export function setupStore({
  hub = createSignalRGameHub(`${API_BASE}/gameHub`),
}: Partial<ThunkExtra> = {}) {
  return configureStore({
    reducer: rootReducer,
    middleware: (getDefaultMiddleware) =>
      getDefaultMiddleware({ thunk: { extraArgument: { hub } } })
        .prepend(createAppListenerMiddleware({ hub }).middleware)
        .concat(api.middleware),
  });
}

export const store = setupStore();

export type RootState = ReturnType<typeof rootReducer>;
export type AppStore = ReturnType<typeof setupStore>;
export type AppDispatch = AppStore["dispatch"];
