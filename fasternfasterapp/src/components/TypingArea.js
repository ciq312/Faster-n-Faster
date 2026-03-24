import { useState, useRef, useCallback } from "react";
import "./TypingArea.css";

function TypingArea({ passage }) {
  const [typed, setTyped] = useState("");
  const inputRef = useRef(null);

  const handleTyping = useCallback(
    (e) => {
      const value = e.target.value;
      if (value.length > typed.length + 1) return;
      if (value.length > passage.length) return;
      if (value.length < typed.length - 1) return;
      setTyped(value);
    },
    [typed.length, passage.length],
  );

  const focusInput = () => inputRef.current?.focus();

  return (
    <>
      <input
        ref={inputRef}
        className="typing-area__hidden-input"
        value={typed}
        onChange={handleTyping}
        onPaste={(e) => e.preventDefault()}
        spellCheck={false}
        autoComplete="off"
        autoCapitalize="off"
      />

      <div className="typing-area" onClick={focusInput}>
        <div className="typing-area__words">
          {passage.split("").map((char, i) => {
            let cls = "typing-area__char";
            if (i < typed.length) {
              cls +=
                typed[i] === char
                  ? " typing-area__char--correct"
                  : " typing-area__char--incorrect";
            }
            return (
              <span key={i} className={cls}>
                {i === typed.length && <span className="typing-area__caret" />}
                {char}
              </span>
            );
          })}
          {typed.length === passage.length && (
            <span className="typing-area__caret typing-area__caret--end" />
          )}
        </div>
      </div>
    </>
  );
}

export default TypingArea;
