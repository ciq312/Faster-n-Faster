import { useNavigate } from "react-router-dom";
import "./AuthCallback.css";

function AuthCallback() {
  const navigate = useNavigate();

  // useEffect(() => {

  // }, []);

  return (
    <div className="auth-callback">
      <p className="auth-callback__text">Signing you in with Google...</p>
    </div>
  );
}

export default AuthCallback;
