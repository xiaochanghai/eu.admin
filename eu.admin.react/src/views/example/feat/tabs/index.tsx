import React, { useState } from "react";
import { useContext } from "react";
import { Button, Card, Divider, Input, Space, Typography } from "antd";
import { HOME_URL } from "@/config";
import { RootState, useDispatch, useSelector } from "@/redux";
import { useLocation, useNavigate } from "react-router-dom";
import { RefreshContext } from "@/context/Refresh";
import { setGlobalState } from "@/redux/modules/global";
import { message } from "@/hooks/useMessage";
import { removeTab, closeMultipleTab, closeTabsOnSide, setTabTitle } from "@/redux/modules/tabs";
import {
  ReloadOutlined,
  ExpandOutlined,
  CompressOutlined,
  CloseCircleOutlined,
  ColumnWidthOutlined,
  SwitcherOutlined,
  VerticalLeftOutlined,
  VerticalRightOutlined,
  SmileOutlined
} from "@ant-design/icons";

const Tabs: React.FC = () => {
  const navigate = useNavigate();
  const dispatch = useDispatch();
  const { pathname, search } = useLocation();

  const path = pathname + search;

  const [value, setValue] = useState("");

  const { updateOutletShow } = useContext(RefreshContext);

  const maximize = useSelector((state: RootState) => state.global.maximize);

  const refreshCurrentPage = () => {
    updateOutletShow(false);
    setTimeout(() => updateOutletShow(true));
  };

  const handleToDetail = (id: string, query: string = "") => {
    navigate(`/feat/tabs/detail/${id}${query}`);
  };

  return (
    <React.Fragment>
      <Card className="mb10">
        <Typography.Title level={4} className="mb20">
          Tab 标题
        </Typography.Title>
        <Space.Compact style={{ width: "350px" }}>
          <Input placeholder="Please enter content" value={value} onChange={e => setValue(e.target.value)} />
          <Button
            type="primary"
            onClick={() => {
              if (!value) return message.warning("请输入 Tab 标题");
              dispatch(setTabTitle(value));
            }}
          >
            Submit
          </Button>
        </Space.Compact>
      </Card>

      <Card className="mb10">
        <Typography.Title level={4} className="mb20">
          Tab 操作
        </Typography.Title>
        <Space className="text">
          <Button type="primary" icon={<ReloadOutlined />} onClick={refreshCurrentPage}>
            刷新当前页
          </Button>
          <Button
            type="primary"
            icon={maximize ? <CompressOutlined /> : <ExpandOutlined />}
            onClick={() => dispatch(setGlobalState({ key: "maximize", value: !maximize }))}
          >
            当前页全屏切换
          </Button>
          <Button type="primary" icon={<CloseCircleOutlined />} onClick={() => dispatch(removeTab({ path, isCurrent: true }))}>
            关闭当前标签页
          </Button>
          <Button
            type="primary"
            icon={<VerticalRightOutlined />}
            onClick={() => dispatch(closeTabsOnSide({ path, type: "left" }))}
          >
            关闭左侧标签页
          </Button>
          <Button
            type="primary"
            icon={<VerticalLeftOutlined />}
            onClick={() => dispatch(closeTabsOnSide({ path, type: "right" }))}
          >
            关闭右侧标签页
          </Button>
          <Button type="primary" icon={<ColumnWidthOutlined />} onClick={() => dispatch(closeMultipleTab({ path }))}>
            关闭其它标签页
          </Button>
          <Button
            type="primary"
            icon={<SwitcherOutlined />}
            onClick={() => {
              dispatch(closeMultipleTab({}));
              navigate(HOME_URL);
            }}
          >
            关闭所有标签页
          </Button>
        </Space>
      </Card>

      <Card>
        <Typography.Title level={4} className="mb20">
          Tab 跳转
        </Typography.Title>
        <Space className="text">
          <Button type="primary" icon={<SmileOutlined />} onClick={() => handleToDetail("1")}>
            打开详情页1
          </Button>
          <Button type="primary" icon={<SmileOutlined />} onClick={() => handleToDetail("2")}>
            打开详情页2
          </Button>
          <Button type="primary" icon={<SmileOutlined />} onClick={() => handleToDetail("3")}>
            打开详情页3
          </Button>
          <Button type="primary" icon={<SmileOutlined />} onClick={() => handleToDetail("4")}>
            打开详情页4
          </Button>
          <Button type="primary" icon={<SmileOutlined />} onClick={() => handleToDetail("5", "?params=detail-page")}>
            打开详情页5 + Query 参数
          </Button>
        </Space>
        <Divider />
        <Button type="primary" icon={<SmileOutlined />} onClick={() => navigate("/noLayout/index")}>
          No layout 空白页
        </Button>
      </Card>
    </React.Fragment>
  );
};

export default Tabs;
