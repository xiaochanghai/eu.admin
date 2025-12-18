import React, { useEffect, useCallback } from "react";
import { Steps, Skeleton } from "antd";
import { downloadFile } from "@/utils";
import { useUploadExcel, useImportTemplate } from "@/hooks/useUploadExcel";
import { useChatUploadExcel } from "@/hooks/useChatUploadExcel";
import { UploadStep, PreviewStep, SuccessStep } from "./components";
import { UploadExcelProps, ChatUploadExcelProps } from "./types";

/**
 * Excel上传组件
 * 功能：支持Excel文件上传、数据预览和导入
 * 流程：上传Excel -> 数据预览 -> 导入数据
 * @param props 组件属性
 */
export const UploadExcel: React.FC<UploadExcelProps> = React.memo(props => {
  const { moduleInfo, onReload } = props;

  // 使用自定义hooks
  const { stepsCurrent, errorList, importColumns, importList, uploading, handleUpload, transferData, resetUpload } =
    useUploadExcel(moduleInfo.moduleCode);

  const { pageLoading, importTemplateInfo, queryTemplate } = useImportTemplate(moduleInfo.moduleId);

  // 组件挂载时查询模板信息
  useEffect(() => {
    queryTemplate();
  }, [queryTemplate]);

  // 处理文件上传
  const handleFileUpload = useCallback(
    async (info: any) => {
      await handleUpload(info.file);
    },
    [handleUpload]
  );

  // 处理数据转换
  const handleTransferData = useCallback(
    async (type: "append" | "override") => {
      const success = await transferData(type, importTemplateInfo.TemplateCode, moduleInfo.masterId, onReload);
      return success;
    },
    [transferData, importTemplateInfo.TemplateCode, moduleInfo.masterId, onReload]
  );

  // 下载模板文件
  const handleDownload = useCallback((fileId: string, templateName: string) => {
    downloadFile(fileId, templateName);
  }, []);

  const items = [
    {
      title: "上传Excel",
      content: (
        <UploadStep
          templateInfo={importTemplateInfo}
          moduleName={moduleInfo.moduleName}
          onUpload={handleFileUpload}
          onDownload={handleDownload}
          uploading={uploading}
        />
      )
    },
    {
      title: "数据预览",
      content: (
        <PreviewStep
          errorList={errorList}
          importList={importList}
          importMasterList={[]}
          importColumns={importColumns}
          templateInfo={importTemplateInfo}
          onTransferData={handleTransferData}
          onReset={resetUpload}
        />
      )
    },
    {
      title: "导入数据",
      content: <SuccessStep onReset={resetUpload} />
    }
  ];
  // 渲染组件
  return (
    <>
      {!pageLoading ? (
        <Steps current={stepsCurrent} titlePlacement="vertical" items={items} ellipsis />
      ) : (
        <>
          <Skeleton />
          <Skeleton />
        </>
      )}
    </>
  );
});

/**
 * Chat Excel上传组件
 * 功能：支持Excel数据预览和导入
 * 流程： 数据预览 -> 导入数据
 * @param props 组件属性
 */
export const ChatUploadExcel: React.FC<ChatUploadExcelProps> = React.memo(props => {
  const { ImportDataId, TemplateId } = props;

  // 使用Chat专用hook
  const {
    stepsCurrent,
    pageLoading,
    errorList,
    importColumns,
    importList,
    importMasterList,
    importTemplateInfo,
    transferChatData,
    resetChatUpload
  } = useChatUploadExcel(ImportDataId, TemplateId);

  // 处理数据转换
  const handleTransferData = useCallback(
    async (type: "append" | "override") => {
      return await transferChatData(type);
    },
    [transferChatData]
  );

  const items = [
    {
      title: "数据预览",
      content: (
        <PreviewStep
          errorList={errorList}
          importList={importList}
          importMasterList={importMasterList}
          importColumns={importColumns}
          templateInfo={importTemplateInfo}
          onTransferData={handleTransferData}
          onReset={resetChatUpload}
          showResetButton={false} // Chat版本不显示返回按钮
        />
      )
    },
    {
      title: "导入数据",
      content: <SuccessStep onReset={resetChatUpload} showResetButton={false} />
    }
  ];
  // 渲染组件
  return (
    <>
      <div style={{ width: 900 }}>
        {!pageLoading ? (
          <Steps current={stepsCurrent} titlePlacement="vertical" items={items} ellipsis />
        ) : (
          <div style={{ padding: 10 }}>
            <Skeleton />
            <Skeleton />
          </div>
        )}
      </div>
    </>
  );
});
