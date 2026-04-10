import * as signalR from "@microsoft/signalr";
import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useRef,
  useState,
} from "react";
import { useError } from "../../shared/components/BannerProvider";
const ConnectionContext = createContext(null);

function ConnectionProvider({ url, children }) {
  const { showError } = useError();
  const connectionRef = useRef(null);
  const [isConnected, setIsConnected] = useState(false);

  useEffect(() => {
    const token = localStorage.getItem("token");
    const connection = new signalR.HubConnectionBuilder()
      .withUrl(url, { accessTokenFactory: () => token })
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
        console.error(`can't connect to ${url}: `, err);
      }
    };

    start();

    subscribe("Error", (error) => {
      showError(error);
    });

    return () => connection.stop();
  }, [url]);

  const invoke = useCallback(async (methodName, ...args) => {
    return connectionRef.current?.invoke(methodName, ...args);
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
