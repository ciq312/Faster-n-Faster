import { useState, useEffect, useRef } from "react";
import connection from "./signalrConnection";
import "./App.css";

function App() {
  const [apiResponse, setApiResponse] = useState(null);
  const [hubStatus, setHubStatus] = useState("Disconnected");
  const [pongTime, setPongTime] = useState(null);
  const [messages, setMessages] = useState([]);
  const nameRef = useRef(null);
  const msgRef = useRef(null);

  useEffect(() => {
    connection.onreconnecting(() => setHubStatus("Reconnecting"));
    connection.onreconnected(() => setHubStatus("Connected"));
    connection.onclose(() => setHubStatus("Disconnected"));

    connection.on("Pong", (timestamp) => setPongTime(timestamp));
    connection.on("ReceiveBroadcast", (user, message) => {
      setMessages((prev) => [...prev, `${user}: ${message}`]);
    });

    connection
      .start()
      .then(() => setHubStatus("Connected"))
      .catch(() => setHubStatus("Failed"));

    return () => connection.stop();
  }, []);

  const pingApi = async () => {
    try {
      const res = await fetch("/api/health");
      const data = await res.json();
      setApiResponse(JSON.stringify(data));
    } catch (err) {
      setApiResponse("Error: " + err.message);
    }
  };

  const pingHub = () => connection.invoke("Ping");

  const disconnect = () => {
    connection.stop();
  };
  const sendBroadcast = () => {
    const user = nameRef.current.value || "Anonymous";
    const message = msgRef.current.value || "";
    if (message) {
      connection.invoke("BroadcastMessage", user, message);
      msgRef.current.value = "";
    }
  };

  return (
    <div className="App">
      <h1>Faster n Faster</h1>

      <section>
        <h3>API Health</h3>
        <button onClick={pingApi}>Ping API</button>
        {apiResponse && <p>{apiResponse}</p>}
      </section>

      <section>
        <h3>SignalR Hub — {hubStatus}</h3>
        <button onClick={pingHub} disabled={hubStatus !== "Connected"}>
          Ping Hub
        </button>
        {pongTime && <p>Pong: {pongTime}</p>}
      </section>

      <section>
        <h3>Broadcast</h3>
        <input ref={nameRef} placeholder="Your name" />
        <input ref={msgRef} placeholder="Message" />
        <button onClick={sendBroadcast} disabled={hubStatus !== "Connected"}>
          Send
        </button>
        <ul>
          {messages.map((msg, i) => (
            <li key={i}>{msg}</li>
          ))}
        </ul>
      </section>
    </div>
  );
}

export default App;
