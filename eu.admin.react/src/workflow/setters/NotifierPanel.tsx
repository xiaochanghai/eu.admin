import { memo, useState } from "react";
// import { VF } from "@src/dsl/VF";
// import FormPage from "@src/pages/common/formPage";
// import { IApproverSettings } from "@/workflow-editor/classes/vlife";
import { FormVo } from "@/api/Form";
import MemberSelect from "@/workflow-editor/components/MemberSelect";
import { IApproverSettings } from "@/workflow-editor/classes/vlife";

// export const NotifierPanel = memo(() => {
export const NotifierPanel = memo((props: { value?: IApproverSettings; formVo?: FormVo; onChange?: (value?: any) => void }) => {
  const [initValue, setInitValue] = useState<any[]>();
  // (props: { value?: IApproverSettings; formVo?: FormVo; onChange?: (value?: IApproverSettings) => void }) => {
  return (
    <>
      抄送
      <MemberSelect
        // read={read || disabled}
        multiple={true}
        value={initValue ?? props.value?.auditList ?? []}
        // value={initValue}
        onDataChange={(data?: any[]) => {
          data = data?.map((f: any) => {
            return { ...f, userType: "notifier" };
          });
          setInitValue(data);
          //仅需要id即可
          props?.onChange?.(data);
        }}
        showUser={true}
        userType="notifier"
      />
    </>
    // <FormPage
    //   terse
    //   fontBold
    //   type="iApproverSettings"
    //   formData={props.value}
    //   onDataChange={props.onChange}
    //   reaction={[
    //     VF.then(
    //       "joinType",
    //       "emptyPass",
    //       "handleType",
    //       "addSign",
    //       "rollback",
    //       "emptyUserId",
    //       "rejected",
    //       "auditLevel",
    //       "recall",
    //       "transfer",
    //       "nodeType",
    //       "entityType",
    //       "passExecuteEl"
    //     ).hide(),
    //     VF.then("nodeType").value("notifier"),
    //     VF.field("handleType").default("general"),
    //     VF.then("auditList").title("抄送至"),
    //     VF.field("fields").default(
    //       props?.formVo?.fields
    //         .filter(f => f.x_hidden !== true)
    //         .map(f => {
    //           return {
    //             title: f.title,
    //             fieldName: f.fieldName,
    //             access: "Readable"
    //           };
    //         })
    //     )
    //     // .componentProps((d, props) => {
    //     //   return {
    //     //     ...props,
    //     //     disableVals: ["Writeable"],
    //     //   };
    //     // }),
    //   ]}
    // />
  );
});
