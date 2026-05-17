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

    const errorHandler = (e) => showError(e);
    const anotherSessionHandler = () => {
      showMessage("Someone else logged into your account");
      navigate("/");
    };
    const bannedHandler = (reason) => {
      showError(reason || "You are banned");
      navigate("/");
    };

    connection.on("Error", errorHandler);
    connection.on("AnotherSessionStarted", anotherSessionHandler);
    connection.on("Banned", bannedHandler);

    const latencyCheckId = setInterval(async () => {
      if (connection.state !== signalR.HubConnectionState.Connected) return;
      try {
        const t0 = performance.now();
        await connection.invoke("Ping", Date.now());
        console.warn(`latency is: ${performance.now() - t0}`);
      } catch {}
    }, 3000);

    const start = async () => {
      try {
        await connection.start();
        setIsConnected(true);
      } catch (err) {
        showError("Can't connect to the server");
        setIsConnected(false);
      }
    };

    start();

    return () => {
      clearInterval(latencyCheckId);
      connection.off("Error", errorHandler);
      connection.off("AnotherSessionStarted", anotherSessionHandler);
      connection.off("Banned", bannedHandler);
      connection.stop();
    };
  }, [url]);

  const invoke = useCallback(
    (methodName, ...args) => connectionRef.current?.invoke(methodName, ...args),
    [],
  );

  const subscribe = useCallback((methodName, callback) => {
    const conn = connectionRef.current;
    conn?.on(methodName, callback);
    return () => conn?.off(methodName, callback);
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
