import * as signalR from "@microsoft/signalr";
import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useRef,
  useState,
} from "react";
import { useNavigate } from "react-router-dom";
import {
  useBannerMessage,
  useError,
} from "../../shared/components/BannerProvider";
import { extractHubError } from "../../shared/utils/extractHubError";
const ConnectionContext = createContext(null);

function ConnectionProvider({ url, children }) {
  const navigate = useNavigate();
  const { showError } = useError();
  const { showMessage } = useBannerMessage();
  const connectionRef = useRef(null);
  const [isConnected, setIsConnected] = useState(false);

  useEffect(() => {
    const connection = new signalR.HubConnectionBuilder()
      .withUrl(url, {
        withCredentials: true,
      })
      .withAutomaticReconnect()
      .build();

    connectionRef.current = connection;

    connection.onclose(() => setIsConnected(false));
    connection.onreconnecting(() => setIsConnected(false));
    connection.onreconnected(() => setIsConnected(true));

    const start = async () => {
      try {
        await connection.start();
        setIsConnected(true);
      } catch (err) {
        showError("Can't connect to the server");
      }
    };

    const errorSub = () => {
      connection.on("Error", (e) => showError(e));
    };

    const anotherSessionSub = () => {
      connection.on("AnotherSessionStarted", () => {
        showMessage("Someone else logged into your account");
        navigate("/");
      });
    };

    const bannedSub = () => {
      connection.on("Banned", (reason) => {
        showError(reason || "You are banned");
        navigate("/");
      });
    };

    start();
    errorSub();
    anotherSessionSub();
    bannedSub();

    return () => {
      connection.stop();
      return connectionRef.current?.off("Error", (e) => showError(e));
    };
  }, [url]);

  const invoke = useCallback(async (methodName, ...args) => {
    try {
      return connectionRef.current?.invoke(methodName, ...args);
    } catch (e) {
      console.log(e);
      showError(extractHubError(e));
    }
  }, []);

  const subscribe = useCallback((methodName, callback) => {
    connectionRef.current?.on(methodName, callback);
    return () => connectionRef.current?.off(methodName, callback);
  }, []);

  const disconnect = useCallback(() => {
    connectionRef.current?.stop();
  }, []);

  return (
    <ConnectionContext.Provider
      value={{
        subscribe,
        invoke,
        disconnect,
        isConnected,
      }}
    >
      {children}
    </ConnectionContext.Provider>
  );
}

export function useConnection() {
  return useContext(ConnectionContext);
}

export default ConnectionProvider;
