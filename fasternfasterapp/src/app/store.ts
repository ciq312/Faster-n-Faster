import { combineSlices, configureStore } from "@reduxjs/toolkit";
import { api } from "../shared/api/api";
import { bannerSlice } from "../shared/banner/bannerSlice";

const rootReducer = combineSlices(api, bannerSlice);

export function setupStore() {
  return configureStore({
    reducer: rootReducer,
    middleware: (getDefaultMiddleware) =>
      getDefaultMiddleware().concat(api.middleware),
  });
}

export const store = setupStore();

export type RootState = ReturnType<typeof rootReducer>;
export type AppStore = ReturnType<typeof setupStore>;
export type AppDispatch = AppStore["dispatch"];
