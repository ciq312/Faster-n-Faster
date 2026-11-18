import { useEffect } from "react";
import { useAppDispatch, useAppSelector } from "../../../app/hooks";
import { selectAuthStatus } from "../../auth/authSelectors";
import { selectIsConnected } from "../../connection/connectionSlice";
import { refreshLobby } from "../../connection/hubThunks";

export function useLobbyRefresh(): void {
  const dispatch = useAppDispatch();
  const isConnected = useAppSelector(selectIsConnected);
  const isAuthenticated = useAppSelector(selectAuthStatus) === "authenticated";

  useEffect(() => {
    if (!isConnected || !isAuthenticated) return;
    dispatch(refreshLobby());
  }, [dispatch, isConnected, isAuthenticated]);
}
