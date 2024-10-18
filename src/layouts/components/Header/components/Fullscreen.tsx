import { useFullscreen } from "ahooks";

const Fullscreen: React.FC = () => {
  const [isFullscreen, { toggleFullscreen }] = useFullscreen(() => document.body);

  return <i className={`iconfont ${isFullscreen ? "icon-suoxiao" : "icon-fangda"}`} onClick={toggleFullscreen}></i>;
};

export default Fullscreen;
