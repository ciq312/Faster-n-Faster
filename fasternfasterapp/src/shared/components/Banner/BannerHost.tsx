import { useEffect, useState } from "react";
import { useAppDispatch, useAppSelector } from "../../../app/hooks";
import {
  bannerDismissed,
  selectCurrentBanner,
  type BannerNote,
} from "../../banner/bannerSlice";
import Banner from "./Banner";

const SHOW_MS = 4000;
const FADE_MS = 400;

function BannerHost() {
  const note = useAppSelector(selectCurrentBanner);
  return note ? <TimedBanner key={note.id} note={note} /> : null;
}

function TimedBanner({ note }: { note: BannerNote }) {
  const dispatch = useAppDispatch();
  const [fading, setFading] = useState(false);

  useEffect(() => {
    const fade = setTimeout(() => setFading(true), SHOW_MS - FADE_MS);
    const remove = setTimeout(
      () => dispatch(bannerDismissed(note.id)),
      SHOW_MS,
    );
    return () => {
      clearTimeout(fade);
      clearTimeout(remove);
    };
  }, [dispatch, note.id]);

  return (
    <Banner variant={note.variant} message={note.message} fading={fading} />
  );
}

export default BannerHost;
