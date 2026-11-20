import { Select } from "antd";
import { pageAnimationOptions, readPageAnimationMode, type PageAnimationMode } from "@/config/pageAnimation";

interface PageAnimationSettingProps {
  value: PageAnimationMode;
  onChange: (value: PageAnimationMode) => void;
}

const PageAnimationSetting = ({ value, onChange }: PageAnimationSettingProps) => (
  <div className="theme-item">
    <span>页面切换动画</span>
    <Select<PageAnimationMode>
      aria-label="页面切换动画"
      style={{ width: 130 }}
      value={readPageAnimationMode(value)}
      options={[...pageAnimationOptions]}
      onChange={onChange}
    />
  </div>
);

export default PageAnimationSetting;
