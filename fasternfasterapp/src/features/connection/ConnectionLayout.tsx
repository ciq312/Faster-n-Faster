import { useEffect } from "react";
import { Outlet, useNavigate } from "react-router-dom";
import { useAppDispatch, useAppSelector } from "../../app/hooks";
import { ROUTES } from "../../shared/utils/routes";
import { selectSessionEndReason } from "./connectionSlice";
import { connectHub, disconnectHub } from "./hubThunks";

function ConnectionLayout() {
  const dispatch = useAppDispatch();
  const navigate = useNavigate();
  const endReason = useAppSelector(selectSessionEndReason);

  useEffect(() => {
    dispatch(connectHub());
    return () => {
      dispatch(disconnectHub());
    };
  }, [dispatch]);

  useEffect(() => {
    if (endReason) navigate(ROUTES.HOME);
  }, [endReason, navigate]);

  return <Outlet />;
}

export default ConnectionLayout;
