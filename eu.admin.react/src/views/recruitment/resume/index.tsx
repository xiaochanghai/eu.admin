import React from "react";
import Main from "../../system/common/main";
import http from "@/api";
import { message } from "@/hooks/useMessage";
import { Modal } from "antd";
const { confirm } = Modal;
import { Icon } from "@/components";

/**
 * 颜色管理模块
 *
 * 该组件使用通用的Main组件加载颜色管理模块信息并渲染对应的表单。
 * Main组件会处理模块信息的获取和状态管理，并根据模块配置渲染适当的界面。
 *
 * 模块代码说明：
 * - RM_RECRUITMENT_RESUME_MNG: 基础数据-简历模块代码
 */
const Index: React.FC = React.memo(() => {
  // eslint-disable-next-line @typescript-eslint/no-unused-vars
  const ResumeRefresh = async (_action: any, _selectedRows: any) => {
    confirm({
      title: "是否确定刷新?",
      icon: <Icon name="ExclamationCircleOutlined" />,
      okText: "确定",
      okType: "danger",
      cancelText: "取消",
      async onOk() {
        const { Success, Message } = await http.post<any>(`/api/RmReume/Refresh`);
        if (Success) message.success(Message);
      },
      onCancel() {
        //
      }
    });
  };

  //#region 操作栏按钮方法
  const action = {
    ResumeRefresh
  };
  return <Main moduleCode="RM_RECRUITMENT_RESUME_MNG" extendAction={action} />;
});

export default Index;
