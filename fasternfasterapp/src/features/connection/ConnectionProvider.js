import * as signalR from "@microsoft/signalr";
import {
  useCallback,
  useEffect,
  useRef,
  useState,
  createContext,
  useContext,
} from "react";

const ConnectionContext = createContext(null);

function ConnectionProvider({ url, children }) {
  const connection = useRef(null);
  const [isConnected, setIsConnected] = useState(false);

  useEffect(() => {
    const connect = async (url) => {
      connection.current = new signalR.HubConnectionBuilder()
        .withUrl(url)
        .withAutomaticReconnect()
        .build();

      try {
        await connection.current.start();
        console.log("connected successfully.");
      } catch (err) {
        console.error(`can't connect to ${url}: `, err);
      }
    };

    connect(url);

    return () => connection.current?.stop();
  }, [url]);

  const invoke = useCallback(async (methodName, ...args) => {
    return await connection.current.invoke(methodName, ...args);
  }, []);

  const subscribe = useCallback((methodName, callback) => {
    if (!connection) return;

    connection.current.on(methodName, callback);

    return () => connection.current.off(methodName, callback);
  }, []);

  const disconnect = useCallback(() => {
    if (!connection) return;
    connection.current.stop();
  }, []);

  connection.current?.onclose(() => setIsConnected(false));
  connection.current?.onreconnecting(() => setIsConnected(false));
  connection.current?.onreconnected(() => setIsConnected(true));

  return (
    <ConnectionContext.Provider
      value={{ subscribe, invoke, disconnect, isConnected }}
    >
      {children}
    </ConnectionContext.Provider>
  );
}

export function useConnection() {
  return useContext(ConnectionContext);
}

export default ConnectionProvider;
