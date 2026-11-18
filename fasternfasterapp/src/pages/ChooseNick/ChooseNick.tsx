import { Link, Navigate, useNavigate } from "react-router-dom";
import { useAppSelector } from "../../app/hooks";
import {
  selectAuthStatus,
  selectCurrentUser,
  selectIsPlayer,
} from "../../features/auth/authSelectors";
import NickForm from "../../features/profile/components/NickForm";
import { ROUTES } from "../../shared/utils/routes";
import "./ChooseNick.css";

function ChooseNick() {
  const navigate = useNavigate();
  const status = useAppSelector(selectAuthStatus);
  const currentUser = useAppSelector(selectCurrentUser);
  const isPlayer = useAppSelector(selectIsPlayer);

  if (status === "loading") return null;
  if (!isPlayer) return <Navigate to={ROUTES.HOME} replace />;

  return (
    <div className="choose-nick">
      <div className="choose-nick__header">
        <h1 className="choose-nick__logo">pick your nickname</h1>
        <p className="choose-nick__tagline">
          you&apos;re playing as {currentUser?.userName}
        </p>
      </div>

      <div className="choose-nick__card">
        <NickForm
          submitLabel="continue"
          onChanged={() => navigate(ROUTES.LOBBIES)}
        />
        <Link to={ROUTES.LOBBIES} className="choose-nick__skip">
          skip for now
        </Link>
      </div>
    </div>
  );
}

export default ChooseNick;
