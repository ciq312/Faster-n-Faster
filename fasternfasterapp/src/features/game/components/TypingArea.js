import { useTyping } from "../hooks/useTyping";
import { useCharPositions } from "../hooks/useCharPositions";
import "./TypingArea.css";

function TypingArea({
  passage,
  disabled,
  onProgress,
  opponents: players = [],
  selfId,
}) {
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
  });

  const { charsRef, containerRef, caretPos } = useCharPositions(passage);

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
          {passage.split("").map((char, i) => {
            let cls = "typing-area__char";
            if (
              (i <= nextSepIndex || nextSepIndex == -1) &&
              i <= typed.length - 1
            ) {
              const isCorrect = typed[i] === char;
              cls += isCorrect
                ? " typing-area__char--correct"
                : " typing-area__char--incorrect";
              cls += i === nextSepIndex ? " typing-area__space--incorrect" : "";
            }
            return (
              <span
                key={i}
                className={cls}
                ref={(el) => (charsRef.current[i] = el)}
              >
                {i === nextSepIndex ? typed.slice(i, typed.length) + " " : char}
              </span>
            );
          })}

          {players.map((p) => {
            if (p.playerId === selfId) {
              const pos = caretPos(typed.length - 1 + 1);
              return (
                <span
                  key={p.id}
                  className="typing-area__caret current__caret"
                  style={{
                    transform: `translate(${pos.left}px, ${pos.top}px)`,
                    background: p.color,
                  }}
                >
                  {/* <span
                    className="typing-area__caret-label"
                    style={{ background: p.color }}
                  >
                    player
                  </span> */}
                </span>
              );
            }
            console.log(selfId, p.playerId, p.index);
            if (p.index < -1 || p.index > passage.length) return null;
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
