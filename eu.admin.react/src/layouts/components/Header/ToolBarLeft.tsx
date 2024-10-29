import CollapseIcon from "./components/CollapseIcon";
import BreadcrumbNav from "./components/BreadcrumbNav";
import "./index.less";

const ToolBarLeft: React.FC = () => {
  return (
    <div className="tool-bar-lf mask-image">
      <CollapseIcon />
      <BreadcrumbNav />
    </div>
  );
};

export default ToolBarLeft;
