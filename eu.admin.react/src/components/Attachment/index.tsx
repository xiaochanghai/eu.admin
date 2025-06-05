import React, { useEffect, useState, useCallback } from "react";
import { Button, Upload, Space, Modal } from "antd";
import { message } from "@/hooks/useMessage";
import { RootState, useSelector, useDispatch } from "@/redux";
import { SmProTable, Loading, Icon } from "@/components";
import { ModuleInfo, ModifyType } from "@/api/interface";
import { setModuleInfo } from "@/redux/modules/module";
import { queryByFilter, uploadFile, getModuleInfo } from "@/api/modules/module";
import { downloadFile } from "@/utils";
import http from "@/api";

import type { UploadFile } from "antd/es/upload/interface";

const { confirm } = Modal;

/**
 * 附件项接口
 */
interface AttachmentItem {
  ID: string;
  OriginalFileName: string;
  CreatedTime: string;
  [key: string]: any;
}

/**
 * 附件组件属性接口
 */
interface AttachmentProps {
  /** 是否禁用 */
  disabled?: boolean;
  /** 接受的文件类型 */
  accept?: string;
  /** 文件存储路径 */
  filePath?: string;
  /** 是否为查看模式 */
  IsView?: boolean;
  /** 主表ID */
  Id?: string | null;
  /** 是否唯一附件 */
  isUnique?: boolean;
  /** 图片类型 */
  imageType?: string;
  /** 自定义删除方法 */
  delete?: (record: AttachmentItem) => void;
  modifyType?: ModifyType;
}

/**
 * 表格操作接口
 */
interface TableAction {
  reload: () => void;
}

// 防止重复上传标志
let uploadFlag = true;

/**
 * 附件管理组件
 *
 * 用于处理文件上传、下载和管理功能
 *
 * @param props 组件属性
 */
const Attachment: React.FC<AttachmentProps> = React.memo(props => {
  // 解构属性，设置默认值
  const {
    accept = ".png,.jpeg",
    filePath = "material",
    IsView,
    Id: MasterId,
    isUnique = false,
    imageType,
    delete: customDelete
  } = props;

  // 状态管理
  const [file, setFile] = useState<UploadFile>();
  const dispatch = useDispatch();
  const moduleCode = "SM_FILE_ATTACHMENT";
  const formRef = React.createRef<any>();

  // 从Redux获取模块信息
  const moduleInfos = useSelector((state: RootState) => state.module.moduleInfos);
  const moduleInfo = moduleInfos[moduleCode] as ModuleInfo;

  /**
   * 获取模块信息
   */
  const fetchModuleInfo = useCallback(async () => {
    try {
      const { Data } = await getModuleInfo(moduleCode);
      if (Data) dispatch(setModuleInfo(Data));
    } catch (error) {
      console.error("获取模块信息失败:", error);
      message.error("获取模块信息失败");
    }
  }, [dispatch, moduleCode]);

  /**
   * 初始化模块信息
   */
  useEffect(() => {
    if (!moduleInfo) fetchModuleInfo();
  }, [moduleInfo, fetchModuleInfo]);

  /**
   * 下载文件
   * @param item 附件项
   */
  const onDownload = useCallback((item: AttachmentItem) => {
    downloadFile(item.ID, item.OriginalFileName);
  }, []);

  /**
   * 上传前处理
   * @param file 上传的文件
   */
  const beforeUpload = useCallback((file: UploadFile) => {
    setFile(file);
    return false; // 阻止自动上传
  }, []);

  /**
   * 上传附件
   * @param action 表格操作对象
   */
  const uploadFileAttachment = useCallback(
    async (action: TableAction) => {
      // 检查必要条件
      if (!file || !MasterId) return false;

      // 防止重复上传
      if (!uploadFlag) return false;

      uploadFlag = false;

      try {
        // 准备表单数据
        const formData = new FormData();
        formData.append("file", file as any);
        formData.append("masterId", MasterId);
        formData.append("filePath", filePath);
        formData.append("imageType", imageType ?? filePath);
        formData.append("isUnique", String(isUnique));

        // 显示上传中提示
        message.loading("附件上传中..", 0);

        // 上传文件
        const { Success } = await uploadFile("/api/File/Upload", formData);

        // 关闭加载提示
        message.destroy();

        if (Success) {
          action.reload();
          message.success("附件上传成功！");
        }
      } catch (error) {
        console.error("上传附件失败:", error);
        message.error("上传附件失败");
      } finally {
        uploadFlag = true; // 重置上传标志
      }
    },
    [file, MasterId, filePath, imageType, isUnique]
  );

  /**
   * 删除附件
   * @param action 表格操作对象
   * @param record 附件记录
   */
  const onOptionDelete = useCallback(
    (action: TableAction, record: AttachmentItem) => {
      confirm({
        title: "是否确定删除该附件?",
        icon: <Icon name="ExclamationCircleOutlined" />,
        okText: "确定",
        okType: "danger",
        cancelText: "取消",
        async onOk() {
          try {
            message.loading("数据提交中...", 0);

            // 使用自定义删除方法或默认删除方法
            if (customDelete) customDelete(record);
            else {
              const { Success, Message } = await http.delete<any>(`/api/File/${record.ID}`);

              if (Success) {
                action.reload();
                message.success(Message);
              }
            }
          } catch (error) {
            console.error("删除附件失败:", error);
            message.error("删除附件失败");
          } finally {
            message.destroy();
          }
        }
      });
    },
    [customDelete]
  );

  // 定义表格列
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
      render: (_: string, item: AttachmentItem) => [
        <a onClick={() => onDownload(item)} key="link">
          {item.OriginalFileName}
        </a>
      ]
    },
    {
      title: "操作",
      dataIndex: "option",
      fixed: "left",
      valueType: "option",
      width: 150,
      render: (_: any, record: AttachmentItem, _index: number, action: TableAction) => [
        <a title="删除" key={record.ID} onClick={() => onOptionDelete(action, record)}>
          <Icon name="DeleteOutlined" />
        </a>
      ]
    }
  ];

  /**
   * 查询附件列表
   */
  const requestAttachments = useCallback(
    async (params: any, sorter: any) => {
      if (!MasterId)
        return {
          data: [],
          success: true,
          total: 0
        };

      const filter = {
        PageIndex: params.current,
        PageSize: params.pageSize,
        sorter,
        params,
        Conditions: `A.ImageType = '${imageType ?? filePath}' AND A.MasterId = '${MasterId}'`
      };

      return await queryByFilter(moduleCode, {}, filter);
    },
    [MasterId, imageType, filePath, moduleCode]
  );

  return (
    <>
      {moduleInfo ? (
        <SmProTable
          columns={columns}
          moduleInfo={moduleInfo}
          search={false}
          toolBarRender={(action: TableAction) => [
            <Space key="upload" style={{ display: "flex", justifyContent: "center" }}>
              <Upload
                accept={accept}
                action=""
                showUploadList={false}
                beforeUpload={beforeUpload}
                onChange={() => uploadFileAttachment(action)}
                disabled={!MasterId || IsView === true}
              >
                <Button type="primary" disabled={!MasterId || IsView === true}>
                  <Icon name="UploadOutlined" /> 上传附件
                </Button>
              </Upload>
            </Space>
          ]}
          formRef={formRef}
          form={{ labelCol: { span: 6 } }}
          request={requestAttachments}
        />
      ) : (
        <div style={{ marginTop: 20 }}>
          <Loading />
        </div>
      )}
    </>
  );
});

export default Attachment;
