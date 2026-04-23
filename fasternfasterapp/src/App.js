import {
  Navigate,
  Route,
  BrowserRouter as Router,
  Routes,
} from "react-router-dom";
import "./App.css";
import ConnectionLayout from "./features/connection/ConnectionLayout";
import AuthCallback from "./pages/AuthCallback/AuthCallback";
import CheckYourEmail from "./pages/CheckYourEmail/CheckYourEmail";
import ForgotPassword from "./pages/ForgotPassword/ForgotPassword";
import Leaderboard from "./pages/Leaderboard/Leaderboard";
import Lobbies from "./pages/Lobbies/Lobbies";
import Lobby from "./pages/Lobby/Lobby";
import Profile from "./pages/Profile/Profile";
import Registration from "./pages/Registration/Registration";
import ResetPassword from "./pages/ResetPassword/ResetPassword";
import VerifyEmail from "./pages/VerifyEmail/VerifyEmail";
import BannerProvider from "./shared/components/BannerProvider";

function App() {
  return (
    <Router>
      <BannerProvider>
        <Routes>
          <Route path="/" element={<Registration />} />
          <Route path="/auth/google/callback" element={<AuthCallback />} />
          <Route element={<ConnectionLayout />}>
            <Route path="/lobbies" element={<Lobbies />} />
            <Route path="/lobby/:lobbyId" element={<Lobby />} />
            <Route path="/profile/me" element={<Profile />} />
            <Route path="/leaderboard" element={<Leaderboard />} />
          </Route>
          <Route path="/verify-email" element={<VerifyEmail />} />
          <Route path="/check-your-email" element={<CheckYourEmail />} />
          <Route path="/forgot-password" element={<ForgotPassword />} />
          <Route path="/reset-password" element={<ResetPassword />} />
          <Route path="*" element={<Navigate to="/" replace />} />
        </Routes>
      </BannerProvider>
    </Router>
  );
}

export default App;
