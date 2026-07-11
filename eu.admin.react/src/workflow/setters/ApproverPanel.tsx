import { memo, useState } from "react";
// import { useTranslate } from "../../workflow-editor/react-locales";
// import { VF } from "@/dsl/VF";
// import FormPage from "@/pages/common/formPage";
import { IApproverSettings } from "@/workflow-editor/classes/vlife";
import { FormVo } from "@/api/Form";
import MemberSelect from "@/workflow-editor/components/MemberSelect";

//materialUis 物料信息配置
export const ApproverPanel = memo((props: { value?: IApproverSettings; formVo?: FormVo; onChange?: (value?: any) => void }) => {
  const [initValue, setInitValue] = useState<any[]>();

  // const changeType = ({ target: { value: AuditSetTypes } }) => {
  //     let data = {
  //         ...config,
  //         AuditSetTypes,
  //         nodeUserList: [],
  //         examineMode: 1,
  //         noHanderAction: 2
  //     }
  // if (settype == 2) {
  //     data.directorLevel = 1;
  // } else if (settype == 4) {
  //     data.selectMode = 1;
  //     data.selectRange = 1;
  // } else if (settype == 7) {
  //     data.examineEndDirectorLevel = 1
  // }
  //     setConfig(data)
  // }
  return (
    <>
      <MemberSelect
        // read={read || disabled}
        multiple={true}
        value={initValue ?? props.value?.auditList ?? []}
        // value={initValue}
        onDataChange={(data?: any[]) => {
          data = data?.map((f: any) => {
            return { ...f, userType: "approver" };
          });
          setInitValue(data);
          //仅需要id即可
          props?.onChange?.(data);
        }}
        showUser={true}
        userType="approver"
      />
      {/* <FormPage
          terse
          fontBold
          type="iApproverSettings"
          formData={props.value}
          onDataChange={props.onChange}
          reaction={[
            VF.then("entityType").value(props?.formVo?.entityType).hide(),
            VF.then("nodeType").value("approver").hide(),
            VF.field("handleType").default("general"),
            VF.field("joinType").default("one_audit"),
            VF.field("handleType").eq("general").then("auditList").show(),
            VF.field("handleType").eq("level").then("auditLevel").show(),
            VF.field("fields").default(
              props?.formVo?.fields
                .filter((f) => f.x_hidden !== true)
                .map((f) => {
                  return {
                    title: f.title,
                    fieldName: f.fieldName,
                    access: "Readable",
                  };
                })
            ),
          ]}
        /> */}
    </>
  );
});
