import React, { useEffect, useState } from "react";
import { Button, Upload, Space } from "antd";
import { message, modal } from "@/hooks/useMessage";
import { RootState, useSelector, useDispatch } from "@/redux";
import { SmProTable, Loading, Icon } from "@/components";
import { ModuleInfo } from "@/api/interface";
import { setModuleInfo } from "@/redux/modules/module";
import { queryByFilter, uploadFile, getModuleInfo } from "@/api/modules/module";
import { downloadFile } from "@/utils";
import http from "@/api";
const { confirm } = modal;

let flag = true;
const Attachment: React.FC<any> = props => {
  let { accept, filePath, IsView, Id: MasterId, isUnique, imageType } = props;
  const [file1, setFile1] = useState<any>();
  const dispatch = useDispatch();
  const moduleCode = "SM_FILE_ATTACHMENT";
  const moduleInfos = useSelector((state: RootState) => state.module.moduleInfos);
  let moduleInfo = moduleInfos[moduleCode] as ModuleInfo;
  // let VITE_API_URL = import.meta.env.VITE_API_URL as string;
  const formRef = React.createRef<any>();

  useEffect(() => {
    const getModuleInfo1 = async () => {
      let { Data } = await getModuleInfo(moduleCode);
      dispatch(setModuleInfo(Data));
    };
    if (!moduleInfo) getModuleInfo1();
  }, []);

  if (!accept) accept = ".png,.jpeg";
  if (!filePath) filePath = "material";

  //#region 操作栏按钮方法
  const action = {};
  //#endregion

  // const onDownload = (item: any) => {
  const onDownload = (item: any) => {
    downloadFile(item.ID, item.OriginalFileName);
  };

  const beforeUpload = (file: any) => {
    // setFileList([...fileList, file]);
    setFile1(file);
    return false;
  };
  const uploadFileAttachment = async (action: any) => {
    if (!filePath) filePath = "material";
    if (!isUnique) isUnique = false;
    if (flag) flag = false;
    else return false;

    //附件上传
    message.loading("附件上传中..", 0);
    const formData = new FormData();
    formData.append("file", file1);
    formData.append("masterId", MasterId);
    formData.append("filePath", filePath);
    formData.append("imageType", imageType ?? filePath);
    formData.append("isUnique", isUnique);
    let { Success } = await uploadFile("/api/File/Upload", formData);

    message.destroy();
    flag = true;
    if (Success) {
      action.reload();
      message.success("附件上传成功！");
    }
  };
  const onOptionDelete = (action: { reload: any }, record: any) => {
    confirm({
      title: "是否确定删除该附件?",
      icon: <Icon name="ExclamationCircleOutlined" />,
      okText: "确定",
      okType: "danger",
      cancelText: "取消",
      async onOk() {
        message.loading("数据提交中...", 0);
        if (props.delete) props.delete(record);
        else {
          let { Success, Message } = await http.delete<any>(`/api/File/${record.ID}`);
          message.destroy();
          if (Success) {
            action.reload();
            message.success(Message);
          }
        }
      },
      onCancel() {
        // console.log('Cancel');
      }
    });
  };
  const columns = [
    {
      title: "创建时间",
      hideInSearch: true,
      dataIndex: "CreatedTime",
      width: 180
    },
    {
      title: "文件名",
      width: 180,
      hideInSearch: true,
      dataIndex: "OriginalFileName",
      render: (reload: any, item: any) => [
        <a onClick={() => onDownload(item)} target="_blank" key="link">
          {reload}
        </a>
      ]
    },
    {
      title: "操作",
      dataIndex: "option",
      fixed: "left",
      valueType: "option",
      width: 150,
      render: (_: any, record: any, _index: number, action: any) => [
        <a title="删除" key={record.id} onClick={() => onOptionDelete(action, record)}>
          <Icon name="DeleteOutlined" />
        </a>
      ]
    }
  ];

  return (
    <>
      {moduleInfo ? (
        <SmProTable
          columns={columns}
          // delete={Delete}
          // batchDelete={selectedRows => BatchDelete(selectedRows)}
          moduleInfo={moduleInfo}
          {...action}
          search={false}
          toolBarRender={(action: any) => [
            <Space style={{ display: "flex", justifyContent: "center" }}>
              <Upload
                accept={accept}
                action={""}
                showUploadList={false}
                beforeUpload={beforeUpload}
                onChange={() => uploadFileAttachment(action)}
                disabled={MasterId && IsView != true ? false : true}
              >
                <Button type="primary" disabled={MasterId && IsView != true ? false : true}>
                  <Icon name="UploadOutlined" /> 上传附件
                </Button>
              </Upload>
            </Space>
          ]}
          // addPage={() => this.setState({ detailVisible: true, DetailId: "", detailView: false })}
          // changePage={Index.changePage}
          // formPage={FormPage}
          formRef={formRef}
          form={{ labelCol: { span: 6 } }}
          // onReset={() => {
          //   dispatch({
          //     type: "attachment/setTableStatus",
          //     payload: {}
          //   });
          // }}
          // onLoad={() => {
          //   if (tableParam && tableParam.params && this.formRef.current) {
          //     this.formRef.current.setFieldsValue({ ...tableParam.params });
          //   }
          // }}
          // pagination={tableParam && tableParam.params ? { current: tableParam.params.current } : {}}
          // eslint-disable-next-line @typescript-eslint/no-unused-vars
          request={async (params: any, sorter: any, _filterCondition: any) => {
            // if (tableParam && tableParam.params && !params._timestamp) {
            //   params = tableParam.params;
            // }
            let filter = {
              PageIndex: params.current,
              PageSize: params.pageSize,
              sorter,
              params,
              Conditions: `A.ImageType = '${imageType ?? filePath}' AND A.MasterId = '${MasterId}'`
            };

            if (MasterId) {
              return await queryByFilter(moduleCode, {}, filter);
            } else
              return {
                data: [],
                success: true,
                total: 0
              };
          }}
        />
      ) : (
        <div style={{ marginTop: 20 }}>
          <Loading />
        </div>
      )}
    </>
  );
};

export default Attachment;
