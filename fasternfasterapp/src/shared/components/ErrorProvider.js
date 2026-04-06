import { createContext, useContext, useState, useCallback } from "react";
import ErrorBanner from "./ErrorBanner";

const ErrorContext = createContext();

function ErrorProvider({ children }) {
  const [error, setError] = useState(null);

  const showError = useCallback((message, duration = 4000) => {
    setError({ message, duration });
  }, []);

  const clearError = useCallback(() => setError(null), []);

  return (
    <ErrorContext.Provider value={{ showError }}>
      {error && (
        <ErrorBanner
          message={error.message}
          duration={error.duration}
          onDismiss={clearError}
        />
      )}
      {children}
    </ErrorContext.Provider>
  );
}

export function useError() {
  return useContext(ErrorContext);
}

export default ErrorProvider;
