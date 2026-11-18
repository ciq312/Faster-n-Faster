import type { SerializedError } from "@reduxjs/toolkit";
import type { FetchBaseQueryError } from "@reduxjs/toolkit/query";
import { useEffect } from "react";
import { useAppDispatch } from "../../app/hooks";
import { errorMessage } from "../api/errorMessage";
import { showError } from "../banner/bannerSlice";

export function useQueryErrorBanner(
  error: FetchBaseQueryError | SerializedError | undefined,
): void {
  const dispatch = useAppDispatch();

  useEffect(() => {
    if (error) dispatch(showError(errorMessage(error)));
  }, [error, dispatch]);
}
