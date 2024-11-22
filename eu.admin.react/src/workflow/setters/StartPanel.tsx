import { memo } from "react";
import React from "react";
// import { Avatar, Space } from "antd";
import { Icon } from "@/components/Icon";
import MemberSelect from "@/workflow-editor/components/MemberSelect";
// import { useTranslate } from "../../workflow-editor/react-locales";
// import { VF } from "@src/dsl/VF";
// import FormPage from "@src/pages/common/formPage";
// import { IApproverSettings } from "@/workflow-editor/classes/vlife";
import { FormVo } from "@/api/Form";
import { Switch } from "antd";
/**
 * // 审批节点信息
export interface AuditInfo extends IModel{
  auditList: NodeUserInfo[];  // 常规审批对象
  emptyPass: string;  // 审批人为空时策略
  handleType: string;  // 办理人员类型
  auditLevel: AuditLevel;  // 逐级审批对象
  transfer:boolean;// 转办
  addSign:boolean;// 加签
  recall:boolean;//撤回
  rollback:boolean;//回退
  rejected:boolean;//拒绝
  fields:FlowField[];//流程字段配置
}
 */

export const StartPanel = memo((props: { value?: any; formVo?: FormVo; onChange?: (value?: any) => void }) => {
  const [initValue, setInitValue] = React.useState<any[]>();
  return (
    <>
      流程发起人
      <MemberSelect
        // read={read || disabled}
        multiple={true}
        value={initValue ?? props.value?.auditList ?? []}
        // value={initValue}
        onDataChange={(data?: any[]) => {
          data = data?.map((f: any) => {
            return { ...f, userType: "assignee" };
          });
          setInitValue(data);
          //仅需要id即可
          props?.onChange?.(data);
        }}
        showUser={true}
      />
      <div className="w-full flex  items-center">
        <div className="w-10 pt-10">
          <Icon name="user-group font-size30 pr-5" />
        </div>
        <div className="flex-1 pt-10">
          <div className="flex justify-between">
            <span className="block font-bold justify-start">流程撤回</span>
            <div className="justify-end semi-switch">
              <Switch />
            </div>
          </div>
          <div className="font-thin text-gray-400 pt-5">节点负责人可对已处理过的待办数据进行撤回</div>
        </div>
      </div>
    </>
  );
});
