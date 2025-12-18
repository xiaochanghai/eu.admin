import { useState, useCallback, useRef } from "react";
import { message } from "@/hooks/useMessage";
import http from "@/api";
import { uploadFile } from "@/api/modules/module";
import { ImportTemplateInfo, ErrorItem, ImportColumn, UploadResponseData, ImportType } from "@/components/UploadExcel/types";
import { validateExcelFile, processImportData, processErrorData } from "@/components/UploadExcel/utils";
import { UPLOAD_STEPS, CHAT_STEPS } from "@/components/UploadExcel/constants";

// 重新导出常量和类型供其他组件使用
export { UPLOAD_STEPS, CHAT_STEPS };
export type { ImportTemplateInfo, ErrorItem, ImportColumn, UploadResponseData };

// 自定义Hook：上传Excel逻辑
export const useUploadExcel = (moduleCode: string) => {
  const [stepsCurrent, setStepsCurrent] = useState<number>(0);
  const [errorList, setErrorList] = useState<ErrorItem[]>([]);
  const [importColumns, setImportColumns] = useState<ImportColumn[]>([]);
  const [importList, setImportList] = useState<Record<string, any>[]>([]);
  const [importDataId, setImportDataId] = useState<string | null>(null);
  const [uploading, setUploading] = useState<boolean>(false);

  const uploadFlag = useRef(true);

  const handleUpload = useCallback(
    async (file: File) => {
      if (!uploadFlag.current || uploading) return false;

      if (!validateExcelFile(file)) {
        return false;
      }

      uploadFlag.current = false;
      setUploading(true);

      try {
        message.loading("上传中..", 0);
        const formData = new FormData();
        formData.append("file", file);
        formData.append("fileName", file.name);

        const { Success, Data, Message } = await uploadFile(`/api/Common/ImportExcel/${moduleCode}`, formData);
        if (Success && Data) {
          if (Data.ErrorList && Data.ErrorList.length > 0) {
            const processedErrors = processErrorData(Data.ErrorList);
            setStepsCurrent(UPLOAD_STEPS.PREVIEW);
            setErrorList(processedErrors);
            return false;
          } else {
            const { processedList, columns } = processImportData(Data.ImportList, Data.ImportColumns, Data.ImportColumnNames);

            setStepsCurrent(UPLOAD_STEPS.PREVIEW);
            setImportList(processedList);
            setImportColumns(columns);
          }
          setImportDataId(Data.ImportDataId);
          message.success(Message ?? "上传成功！", 3);
          return true;
        } else {
          return false;
        }
      } catch (error) {
        console.error("上传失败:", error);
        message.error("上传过程中发生错误");
        return false;
      } finally {
        message.destroy();
        uploadFlag.current = true;
        setUploading(false);
      }
    },
    [moduleCode, uploading]
  );

  const transferData = useCallback(
    async (type: ImportType, templateCode: string, masterId?: string, onSuccess?: () => void) => {
      if (!importDataId) {
        message.error("导入数据ID不存在，请重新上传");
        return false;
      }

      try {
        message.loading("数据转换中..", 0);
        const { Success, Message } = await http.post<any>(`/api/Common/TransferExcelData`, {
          type,
          importDataId,
          importTemplateCode: templateCode,
          masterId: masterId ?? null
        });
        if (Success) {
          onSuccess?.();
          setStepsCurrent(UPLOAD_STEPS.SUCCESS);
          message.success(Message || "导入成功", 3);
          return true;
        } else {
          message.error(Message || "导入失败", 3);
          return false;
        }
      } catch (error) {
        console.error("数据转换失败:", error);
        message.error("数据转换过程中发生错误");
        return false;
      } finally {
        message.destroy();
      }
    },
    [importDataId, moduleCode]
  );

  const resetUpload = useCallback(() => {
    setStepsCurrent(UPLOAD_STEPS.UPLOAD);
    setImportDataId(null);
    setErrorList([]);
    setImportList([]);
    setImportColumns([]);
  }, []);

  return {
    stepsCurrent,
    errorList,
    importColumns,
    importList,
    importDataId,
    uploading,
    handleUpload,
    transferData,
    resetUpload,
    setStepsCurrent
  };
};

// 自定义Hook：导入模板管理
export const useImportTemplate = (moduleId?: string) => {
  const [pageLoading, setPageLoading] = useState<boolean>(true);
  const [importTemplateInfo, setImportTemplateInfo] = useState<ImportTemplateInfo>({
    TemplateCode: "",
    FileId: "",
    TemplateName: "",
    IsAllowOverride: true
  });

  const queryTemplate = useCallback(async () => {
    if (!moduleId) return;

    try {
      setPageLoading(true);
      const { Success, Data } = await http.get<ImportTemplateInfo>(`/api/SmImpTemplate/QueryByModuleId/${moduleId}`);

      if (Success && Data) {
        setImportTemplateInfo(Data);
      } else {
        message.warning("未找到导入模板配置");
      }
    } catch (error) {
      console.error("查询模板失败:", error);
      message.error("获取模板信息失败");
    } finally {
      setPageLoading(false);
    }
  }, [moduleId]);

  const queryChatTemplate = useCallback(async (importDataId: string, templateId: string) => {
    try {
      setPageLoading(true);
      const { Success, Data } = await http.get<any>(`/api/Common/QueryImportExcelResult/${importDataId}/${templateId}`);

      if (Success && Data) {
        const { ImportList, ImportColumns, ImportColumnNames, Template, moduleCode } = Data;
        const { processedList, columns } = processImportData(ImportList, ImportColumns, ImportColumnNames);

        setImportTemplateInfo(Template);
        return {
          importList: processedList,
          importColumns: columns,
          moduleCode
        };
      } else {
        message.warning("未找到导入模板配置");
        return null;
      }
    } catch (error) {
      console.error("查询模板失败:", error);
      message.error("获取模板信息失败");
      return null;
    } finally {
      setPageLoading(false);
    }
  }, []);

  return {
    pageLoading,
    importTemplateInfo,
    queryTemplate,
    queryChatTemplate
  };
};
