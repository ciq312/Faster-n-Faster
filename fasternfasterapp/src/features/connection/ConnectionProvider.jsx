import * as signalR from "@microsoft/signalr";
import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useEffectEvent,
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

  const onAnotherSessionStarted = useEffectEvent(() => {
    showMessage("Someone else logged into your account");
    navigate("/");
  });
  const onBanned = useEffectEvent((reason) => {
    showError(reason || "You are banned");
    navigate("/");
  });
  const onSuspended = useEffectEvent((data) => {
    showError(
      `You are suspended until ${new Date(data.expiresAt).toLocaleString()}`,
    );
    navigate("/");
  });
  const onStartFailed = useEffectEvent(() => {
    showError("Can't connect to the server");
  });

  useEffect(() => {
    const connection = new signalR.HubConnectionBuilder()
      .withUrl(url, {
        withCredentials: true,
      })
      .withAutomaticReconnect()
      .build();

    let ignore = false;
    connectionRef.current = connection;

    connection.onclose(() => {
      if (!ignore) setIsConnected(false);
    });
    connection.onreconnecting(() => {
      if (!ignore) setIsConnected(false);
    });
    connection.onreconnected(() => {
      if (!ignore) setIsConnected(true);
    });

    const anotherSessionHandler = () => onAnotherSessionStarted();
    const bannedHandler = (reason) => onBanned(reason);
    const suspendedHandler = (data) => {
      connection.stop();
      onSuspended(data);
    };

    connection.on("AnotherSessionStarted", anotherSessionHandler);
    connection.on("Banned", bannedHandler);
    connection.on("Suspended", suspendedHandler);

    const latencyCheckId = import.meta.env.DEV
      ? setInterval(async () => {
          if (connection.state !== signalR.HubConnectionState.Connected) {
            return;
          }
          try {
            const t0 = performance.now();
            await connection.invoke("Ping", Date.now());
            console.debug(`latency: ${Math.round(performance.now() - t0)}ms`);
          } catch {
            console.log("latency check failed");
          }
        }, 3000)
      : null;

    const start = async () => {
      try {
        await connection.start();
        if (!ignore) setIsConnected(true);
      } catch {
        if (ignore) return;
        onStartFailed();
        setIsConnected(false);
      }
    };

    start();

    return () => {
      ignore = true;
      clearInterval(latencyCheckId);
      connection.off("AnotherSessionStarted", anotherSessionHandler);
      connection.off("Banned", bannedHandler);
      connection.off("Suspended", suspendedHandler);
      connectionRef.current = null;
      setIsConnected(false);
      connection.stop();
    };
  }, [url]);

  const invoke = useCallback(
    (methodName, ...args) => connectionRef.current?.invoke(methodName, ...args),
    [],
  );

  const invokeOrShowError = useCallback(
    async (methodName, ...args) => {
      try {
        return await invoke(methodName, ...args);
      } catch (e) {
        showError(extractHubError(e));
      }
    },
    [invoke, showError],
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
        invokeOrShowError,
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
