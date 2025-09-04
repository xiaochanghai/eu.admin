import React from "react";
import logo from "@/assets/images/logo.png";
const APP_TITLE = import.meta.env.VITE_GLOB_APP_TITLE;
import { useStyle } from "./Styles";

export const AvatarIcon: React.FC<any> = React.memo(() => {
  const { styles } = useStyle();
  return (
    <React.Fragment>
      <div className={styles.logo}>
        <img src={logo} draggable={false} alt="logo" width={24} />
        <span>{APP_TITLE}</span>
      </div>
    </React.Fragment>
  );
});
