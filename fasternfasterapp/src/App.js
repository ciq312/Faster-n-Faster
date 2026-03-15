import { useState } from "react";
import "./App.css";

function App() {
  const [response, setResponse] = useState(null);

  const pingApi = async () => {
    try {
      const res = await fetch("/api/health");
      const data = await res.json();
      setResponse(JSON.stringify(data));
    } catch (err) {
      setResponse("Error: " + err.message);
    }
  };

  return (
    <div className="App">
      <h1>Faster n Faster</h1>
      <button onClick={pingApi}>Ping API</button>
      {response && <p>{response}</p>}
    </div>
  );
}

export default App;
