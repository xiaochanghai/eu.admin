import { Badge, Popover, Tabs, Empty } from "antd";
import type { TabsProps } from "antd";

interface IconImports {
  [path: string]: () => Promise<{ default: string }>;
}

interface Icons {
  [path: string]: string;
}

const iconsImports = import.meta.glob("/src/assets/images/notice*.png") as IconImports;

let icons: Icons = {};

for (const path in iconsImports) {
  iconsImports[path]().then(module => (icons[path] = module.default));
}

const Notice: React.FC = () => {
  const noticeList = [
    { title: "一键三连 🧡", icon: "notice01.png", time: "一分钟前" },
    { title: "一键三连 💙", icon: "notice02.png", time: "一小时前" },
    { title: "一键三连 💚", icon: "notice03.png", time: "半天前" },
    { title: "一键三连 💜", icon: "notice04.png", time: "一星期前" },
    { title: "一键三连 💛", icon: "notice05.png", time: "一个月前" }
  ];

  return (
    <div className="notice-list">
      {noticeList.map(item => {
        return (
          <div className="notice-item" key={item.title}>
            <img src={icons[`/src/assets/images/${item.icon}`]} alt="" className="notice-icon" />
            <div className="notice-content">
              <span className="notice-title">{item.title}</span>
              <span className="notice-time">{item.time}</span>
            </div>
          </div>
        );
      })}
    </div>
  );
};

const Message: React.FC = () => {
  const items: TabsProps["items"] = [
    {
      key: "1",
      label: `通知 (${5})`,
      children: <Notice />
    },
    {
      key: "2",
      label: `消息 (${0})`,
      children: <Empty className="pt60 pb90" description="暂无消息" />
    },
    {
      key: "3",
      label: `代办 (${0})`,
      children: <Empty className="pt60 pb90" description="暂无待办" />
    }
  ];

  const content = <Tabs defaultActiveKey="1" size="middle" tabBarGutter={50} className="pr12 pl12" items={items} />;

  return (
    <Popover placement="bottom" content={content} trigger="click" overlayClassName="message-popover">
      <Badge count={5} style={{ color: "#ffffff" }}>
        <i className="iconfont icon-xiaoxi"></i>
      </Badge>
    </Popover>
  );
};
export default Message;
