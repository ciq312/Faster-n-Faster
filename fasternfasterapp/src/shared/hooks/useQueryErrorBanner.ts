import type { SerializedError } from "@reduxjs/toolkit";
import type { FetchBaseQueryError } from "@reduxjs/toolkit/query";
import { useEffect } from "react";
import { errorMessage } from "../api/errorMessage";
import { useError } from "../components/BannerProvider";

export function useQueryErrorBanner(
  error: FetchBaseQueryError | SerializedError | undefined,
): void {
  const { showError } = useError();

  useEffect(() => {
    if (error) showError(errorMessage(error));
  }, [error, showError]);
}
