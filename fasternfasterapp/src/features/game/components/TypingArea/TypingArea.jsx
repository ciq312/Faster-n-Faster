import { useEffect, useLayoutEffect, useRef, useState } from "react";
import { useCharPositions } from "../../hooks/useCharPositions";
import { useTyping } from "../../hooks/useTyping";
import "./TypingArea.css";

function TypingArea({
  passage,
  disabled,
  onProgress,
  opponents: players = [],
  selfId,
}) {

  const [self, setSelf] = useState(null);
  
  useEffect(() => {
    setSelf(players.filter((p) => p.playerId === selfId).pop());
  }, [players]);
  

  const {
    typed,
    inputRef,
    handleTyping,
    focusInput,
    lastCorrectIndex,
    nextSepIndex,
  } = useTyping({
    passage,
    disabled,
    onProgress,
    selfTyped: self?.typed,
    selfCorrectIndex: self?.index,
    selfMistakes: self?.mistakes,
  });
  const { charsRef, containerRef, caretPos } = useCharPositions(
    passage,
    typed.length,
  );

  const lastOverflowRef = useRef(null);
  const [selfPos, setSelfPos] = useState({ left: 0, top: 0 });

  useLayoutEffect(() => {
    if (lastOverflowRef.current && containerRef.current) {
      const rect = lastOverflowRef.current.getBoundingClientRect();
      const containerRect = containerRef.current.getBoundingClientRect();
      setSelfPos({
        left: rect.right - containerRect.left,
        top: rect.top - containerRect.top,
      });
    } else {
      setSelfPos(caretPos(typed.length));
    }
  }, [typed.length, caretPos]);

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

      <div className={`typing-area`} onClick={focusInput}>
        <div className="typing-area__words" ref={containerRef}>
          {passage.split("").flatMap((char, i) => {
            if (i === nextSepIndex && typed.length > i) {
              const overflowChars = typed.slice(i).split("");
              lastOverflowRef.current = null;
              const spans = overflowChars.map((c, j) => (
                <span
                  key={`o${j}`}
                  className="typing-area__char typing-area__char--incorrect"
                  ref={
                    j === overflowChars.length - 1
                      ? (el) => (lastOverflowRef.current = el)
                      : undefined
                  }
                >
                  {c}
                </span>
              ));
              spans.push(
                <span
                  key={i}
                  className="typing-area__char typing-area__space--incorrect"
                  ref={(el) => (charsRef.current[i] = el)}
                >
                  {" "}
                </span>,
              );
              return spans;
            }

            if (i === nextSepIndex && typed.length <= i) {
              lastOverflowRef.current = null;
            }

            let cls = "typing-area__char";
            if (
              (i <= nextSepIndex || nextSepIndex === -1) &&
              i < typed.length
            ) {
              cls +=
                typed[i] === char
                  ? " typing-area__char--correct"
                  : " typing-area__char--incorrect";
            }
            return [
              <span
                key={i}
                className={cls}
                ref={(el) => (charsRef.current[i] = el)}
              >
                {char}
              </span>,
            ];
          })}

          {players.map((p) => {
            if (p.index < -1 || p.index >= passage.length - 1) return null;

            if (p.playerId === selfId) {
              return (
                <span
                  key={p.id}
                  className="typing-area__caret current"
                  style={{
                    transform: `translate(${selfPos.left}px, ${selfPos.top}px)`,
                    background: p.color,
                  }}
                >
                  <span
                    className="typing-area__caret-label current"
                    style={{ background: p.color }}
                  >
                    you
                  </span>
                </span>
              );
            }
            const pos = caretPos(p.index + 1);
            return (
              <span
                key={p.id}
                className="typing-area__caret"
                style={{
                  transform: `translate(${pos.left}px, ${pos.top}px)`,
                  background: p.color,
                }}
              >
                <span
                  className="typing-area__caret-label"
                  style={{ background: p.color }}
                >
                  {p.nick}
                </span>
              </span>
            );
          })}
        </div>
      </div>
    </>
  );
}

export default TypingArea;
