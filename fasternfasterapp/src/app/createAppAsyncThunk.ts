import { createAsyncThunk } from "@reduxjs/toolkit";
import type { AppDispatch, RootState, ThunkExtra } from "./store";

export type AppThunkConfig = {
  state: RootState;
  dispatch: AppDispatch;
  extra: ThunkExtra;
  rejectValue: string;
};

export const createAppAsyncThunk = createAsyncThunk.withTypes<AppThunkConfig>();
