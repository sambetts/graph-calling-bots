import { useContext } from "react";
import { TeamsFxContext } from "./Context";
import { AppPageContents } from "./sample/AppPageContents";
import './Tab.css'

export default function Tab() {
  const { themeString } = useContext(TeamsFxContext);
  return (
    <div
      className={themeString === "default" ? "light" : themeString === "dark" ? "dark" : "contrast"}
    >
      <AppPageContents />
    </div>
  );
}
