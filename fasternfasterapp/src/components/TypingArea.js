import { useState, useRef, useCallback, useEffect } from "react";
import "./TypingArea.css";

function TypingArea({ passage, disabled, onProgress, opponents = [] }) {
  const [typed, setTyped] = useState("");
  const mistakesRef = useRef(0);
  const inputRef = useRef(null);

  const handleTyping = useCallback(
    (e) => {
      if (disabled) return;
      const value = e.target.value;
      if (value.length > typed.length + 1) return;
      if (value.length > passage.length) return;
      if (value.length < typed.length - 1) return;

      if (value.length > typed.length) {
        const newChar = value[value.length - 1];
        if (newChar === passage[value.length - 1]) {
          setTyped(value);
        } else {
          mistakesRef.current++;
        }
      }

      onProgress?.({
        index: value.length,
        totalTyped: value.length,
        mistakes: mistakesRef.current,
      });
    },
    [typed.length, passage, disabled, onProgress],
  );

  useEffect(() => {
    if (!disabled) inputRef.current?.focus();
  }, [disabled]);

  const focusInput = () => !disabled && inputRef.current?.focus();

  const opponentsByIndex = {};
  for (const op of opponents) {
    if (op.index >= 0 && op.index <= passage.length) {
      if (!opponentsByIndex[op.index]) opponentsByIndex[op.index] = [];
      opponentsByIndex[op.index].push(op);
    }
  }

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
        disabled={disabled}
      />

      <div
        className={`typing-area ${disabled ? "typing-area--disabled" : ""}`}
        onClick={focusInput}
      >
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
                {opponentsByIndex[i] &&
                  opponentsByIndex[i].map((op) => (
                    <span key={op.id} className="typing-area__opponent-caret">
                      <span className="typing-area__opponent-label">
                        {op.nick}
                      </span>
                    </span>
                  ))}
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
