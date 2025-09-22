import React from "react";

import UserName from "./components/UserName";
import AvatarIcon from "./components/AvatarIcon";
import ComponentSize from "./components/ComponentSize";
import Language from "./components/Language";
import SearchMenu from "./components/SearchMenu";
import ThemeSetting from "./components/ThemeSetting";
import Message from "./components/Message";
import Fullscreen from "./components/Fullscreen";
import "./index.less";

interface ToolBarRightProps {
  layout?: string;
}

const ToolBarRight: React.FC<ToolBarRightProps> = React.memo(({ layout }) => {
  return (
    <div className="tool-bar-ri">
      <div className="header-icon">
        <ComponentSize />
        <Language />
        <SearchMenu />
        <ThemeSetting />
        <Message />
        <Fullscreen />
      </div>
      <UserName />
      <AvatarIcon layout={layout} />
    </div>
  );
});

export default React.memo(ToolBarRight);
