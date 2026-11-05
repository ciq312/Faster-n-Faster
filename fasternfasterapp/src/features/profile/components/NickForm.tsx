import { useState, type FormEvent } from "react";
import { useAppDispatch } from "../../../app/hooks";
import { errorMessage } from "../../../shared/api/errorMessage";
import { showError } from "../../../shared/banner/bannerSlice";
import { NICK_MAX_LENGTH } from "../nickRules";
import { useChangeNickMutation } from "../profileApi";
import "./NickForm.css";

type NickFormProps = {
  submitLabel: string;
  onChanged: () => void;
};

function NickForm({ submitLabel, onChanged }: NickFormProps) {
  const dispatch = useAppDispatch();
  const [nick, setNick] = useState("");
  const [changeNick, { isLoading }] = useChangeNickMutation();

  const handleSubmit = async (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    const result = await changeNick({ nick });
    if (result.error) dispatch(showError(errorMessage(result.error)));
    else onChanged();
  };

  return (
    <form className="nick-form" onSubmit={handleSubmit}>
      <input
        className="nick-form__input"
        type="text"
        value={nick}
        onChange={(e) => setNick(e.target.value)}
        placeholder="enter a nickname"
        maxLength={NICK_MAX_LENGTH}
        autoFocus
      />
      <button
        className="nick-form__submit"
        type="submit"
        disabled={!nick.trim() || isLoading}
      >
        {isLoading ? "saving..." : submitLabel}
      </button>
    </form>
  );
}

export default NickForm;
