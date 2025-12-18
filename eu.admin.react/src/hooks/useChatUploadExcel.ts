import { useState, useCallback, useEffect } from "react";
import { message } from "@/hooks/useMessage";
import http from "@/api";
import { ImportTemplateInfo, ErrorItem, ImportColumn, ImportType, ChatQueryResponseData } from "@/components/UploadExcel/types";
import { processImportData, processErrorData } from "@/components/UploadExcel/utils";
import { CHAT_STEPS } from "@/components/UploadExcel/constants";

/**
 * Chat Excel上传专用Hook
 * 处理Chat场景下的Excel数据预览和导入
 */
export const useChatUploadExcel = (importDataId: string, templateId: string) => {
  const [stepsCurrent, setStepsCurrent] = useState<number>(0);
  const [pageLoading, setPageLoading] = useState<boolean>(true);
  const [errorList, setErrorList] = useState<ErrorItem[]>([]);
  const [importColumns, setImportColumns] = useState<ImportColumn[]>([]);
  const [importList, setImportList] = useState<Record<string, any>[]>([]);
  const [importMasterList, setImportMasterList] = useState<Record<string, any>[]>([]);
  const [importTemplateInfo, setImportTemplateInfo] = useState<ImportTemplateInfo>({
    TemplateCode: "",
    FileId: "",
    TemplateName: "",
    IsAllowOverride: true
  });
  const [moduleCode, setModuleCode] = useState<string>("");

  /**
   * 查询Chat导入数据
   */
  const queryChatData = useCallback(async () => {
    try {
      setPageLoading(true);
      const { Success, Data } = await http.get<ChatQueryResponseData>(
        `/api/Common/QueryImportExcelResult/${importDataId}/${templateId}`
      );

      if (Success && Data) {
        const { ImportList, ImportColumns, ImportColumnNames, Template, moduleCode, ErrorList, ImportMasterList } = Data;
        if (ErrorList && ErrorList.length > 0) {
          const processedErrors = processErrorData(ErrorList);
          setStepsCurrent(CHAT_STEPS.PREVIEW);
          setErrorList(processedErrors);
          return false;
        } else {
          // 处理数据
          const { processedList, columns } = processImportData(ImportList, ImportColumns, ImportColumnNames);

          setModuleCode(moduleCode);
          setStepsCurrent(CHAT_STEPS.PREVIEW);
          setImportList(processedList);
          setImportMasterList(ImportMasterList);
          setImportColumns(columns);
          setImportTemplateInfo(Template);
        }
      } else {
        message.warning("未找到导入模板配置");
      }
    } catch (error) {
      console.error("查询Chat模板失败:", error);
      message.error("获取模板信息失败");
    } finally {
      setPageLoading(false);
    }
  }, [importDataId, templateId]);

  /**
   * Chat数据转换处理
   */
  const transferChatData = useCallback(
    async (type: ImportType) => {
      if (!importDataId) {
        message.error("导入数据ID不存在，请重新上传");
        return false;
      }

      try {
        message.loading("数据转换中..", 0);
        const response = await http.post<any>(`/api/Common/TransferExcelData`, {
          type,
          importDataId,
          importTemplateCode: importTemplateInfo?.TemplateCode
        });

        const { Success, Message } = response;

        if (Success) {
          setStepsCurrent(CHAT_STEPS.SUCCESS);
          message.success(Message || "导入成功", 3);
          return true;
        } else {
          message.error(Message || "导入失败", 3);
          return false;
        }
      } catch (error) {
        console.error("Chat数据转换失败:", error);
        message.error("数据转换过程中发生错误");
        return false;
      } finally {
        message.destroy();
      }
    },
    [importDataId, importTemplateInfo, moduleCode]
  );

  /**
   * 重置状态
   */
  const resetChatUpload = useCallback(() => {
    setStepsCurrent(CHAT_STEPS.PREVIEW);
    setErrorList([]);
  }, []);

  // 组件挂载时自动查询数据
  useEffect(() => {
    if (importDataId && templateId) {
      queryChatData();
    }
  }, [queryChatData, importDataId, templateId]);

  return {
    stepsCurrent,
    pageLoading,
    errorList,
    importColumns,
    importList,
    importMasterList,
    importTemplateInfo,
    transferChatData,
    resetChatUpload
  };
};
