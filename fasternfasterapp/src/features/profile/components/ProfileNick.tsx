import { useEffect, useState, type FormEvent, type KeyboardEvent } from "react";
import { useAppDispatch } from "../../../app/hooks";
import { errorMessage } from "../../../shared/api/errorMessage";
import { showError, showMessage } from "../../../shared/banner/bannerSlice";
import { NICK_MAX_LENGTH } from "../nickRules";
import { useChangeNickMutation } from "../profileApi";
import "./ProfileNick.css";

const SAVED_DISPLAY_MS = 1000;

type Mode = "viewing" | "editing" | "saved";

const BUTTON_LABELS: Record<Mode, string> = {
  viewing: "edit nick",
  editing: "save nick",
  saved: "nick saved",
};

type ProfileNickProps = {
  nick: string;
  editable: boolean;
  onChanged: () => void;
};

function PencilIcon() {
  return (
    <svg className="profile-nick__icon" viewBox="0 0 24 24" aria-hidden="true">
      <path d="M4 20h4L19 9l-4-4L4 16v4z" />
    </svg>
  );
}

function CheckIcon() {
  return (
    <svg className="profile-nick__icon" viewBox="0 0 24 24" aria-hidden="true">
      <path d="M5 13l4 4L19 7" />
    </svg>
  );
}

function ProfileNick({ nick, editable, onChanged }: ProfileNickProps) {
  const dispatch = useAppDispatch();
  const [mode, setMode] = useState<Mode>("viewing");
  const [draft, setDraft] = useState(nick);
  const [changeNick, { isLoading }] = useChangeNickMutation();

  useEffect(() => {
    if (mode !== "saved") return;
    const timer = setTimeout(() => setMode("viewing"), SAVED_DISPLAY_MS);
    return () => clearTimeout(timer);
  }, [mode]);

  const startEditing = () => {
    setDraft(nick);
    setMode("editing");
  };

  const save = async () => {
    if (isLoading || !draft.trim()) return;
    if (draft === nick) {
      setMode("viewing");
      return;
    }
    const result = await changeNick({ nick: draft });
    if (result.error) {
      dispatch(showError(errorMessage(result.error)));
      return;
    }
    dispatch(showMessage(`You changed nick to ${draft}`));
    setMode("saved");
    onChanged();
  };

  const handleSubmit = (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    save();
  };

  const handleKeyDown = (e: KeyboardEvent<HTMLInputElement>) => {
    if (e.key === "Escape" && !isLoading) setMode("viewing");
  };

  const isEditing = mode === "editing";

  return (
    <form className="profile-nick" onSubmit={handleSubmit}>
      {isEditing ? (
        <input
          className="profile-nick__input"
          type="text"
          value={draft}
          onChange={(e) => setDraft(e.target.value)}
          onKeyDown={handleKeyDown}
          maxLength={NICK_MAX_LENGTH}
          readOnly={isLoading}
          aria-label="nick"
          autoFocus
        />
      ) : (
        <h1 className="profile-nick__text">
          {mode === "saved" ? draft : nick}
        </h1>
      )}
      {editable && (
        <button
          className="profile-nick__button"
          type="button"
          onClick={isEditing ? save : startEditing}
          disabled={
            isLoading || mode === "saved" || (isEditing && !draft.trim())
          }
          aria-label={isLoading ? "saving nick" : BUTTON_LABELS[mode]}
        >
          {isLoading ? (
            <span className="profile-nick__spinner" />
          ) : mode === "viewing" ? (
            <PencilIcon />
          ) : (
            <CheckIcon />
          )}
        </button>
      )}
    </form>
  );
}

export default ProfileNick;
