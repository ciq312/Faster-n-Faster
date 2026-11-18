import { createSlice, nanoid, type PayloadAction } from "@reduxjs/toolkit";

export type BannerVariant = "error" | "message";
export type BannerNote = {
  id: string;
  variant: BannerVariant;
  message: string;
};

type BannerState = { current: BannerNote | null; next: BannerNote | null };

const initialState: BannerState = { current: null, next: null };

function enqueue(state: BannerState, { payload }: PayloadAction<BannerNote>) {
  if (state.current) state.next = payload;
  else state.current = payload;
}

function prepareNote(variant: BannerVariant) {
  return (message: string) => ({
    payload: { id: nanoid(), variant, message },
  });
}

export const bannerSlice = createSlice({
  name: "banner",
  initialState,
  reducers: {
    showError: { reducer: enqueue, prepare: prepareNote("error") },
    showMessage: { reducer: enqueue, prepare: prepareNote("message") },
    bannerDismissed: (state, { payload: id }: PayloadAction<string>) => {
      if (state.current?.id !== id) return;
      state.current = state.next;
      state.next = null;
    },
  },
  selectors: {
    selectCurrentBanner: (state) => state.current,
  },
});

export const { showError, showMessage, bannerDismissed } = bannerSlice.actions;
export const { selectCurrentBanner } = bannerSlice.selectors;
