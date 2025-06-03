import React, { useEffect, useState, useCallback } from "react";
import { Upload, Form, Row, Col, Steps, Button, Space, Result, Table } from "antd";
import { message } from "@/hooks/useMessage";
import { downloadFile } from "@/utils";
import http from "@/api";
import { uploadFile } from "@/api/modules/module";
import { Icon } from "@/components";
import { Skeleton } from "antd";
const { Step } = Steps;
const FormItem = Form.Item;

/**
 * 导入模板信息接口
 */
interface ImportTemplateInfo {
  /** 模板代码 */
  TemplateCode: string;
  /** 文件ID */
  FileId: string;
  /** 模板名称 */
  TemplateName: string;
  /** 是否允许覆盖导入 */
  IsAllowOverride: boolean;
}

/**
 * 错误信息接口
 */
interface ErrorItem {
  /** 序号 */
  Key: number;
  /** Sheet名称 */
  SheetName: string;
  /** 错误信息 */
  ErrorName: string;
}

/**
 * 导入列定义接口
 */
interface ImportColumn {
  /** 列标题 */
  title: string;
  /** 数据索引 */
  dataIndex: string;
  /** 列键值 */
  key: string;
}

/**
 * 模块信息接口
 */
interface ModuleInfo {
  /** 主记录ID */
  masterId?: string;
  /** 模块代码 */
  moduleCode: string;
  /** 模块ID */
  moduleId: string;
  /** 模块名称 */
  moduleName: string;
}

/**
 * 组件属性接口
 */
interface UploadExcelProps {
  /** 模块信息 */
  moduleInfo: ModuleInfo;
  /** 数据重新加载回调 */
  onReload: () => void;
  /** 取消操作回调 */
  onCancel: () => void;
}

/**
 * Excel上传组件
 * 功能：支持Excel文件上传、数据预览和导入
 * 流程：上传Excel -> 数据预览 -> 导入数据
 * @param props 组件属性
 */
const UploadExcel: React.FC<UploadExcelProps> = props => {
  // 步骤状态
  const [stepsCurrent, setStepsCurrent] = useState<number>(0);
  // 页面加载状态
  const [pageLoading, setPageLoading] = useState<boolean>(true);
  // 错误列表
  const [errorList, setErrorList] = useState<ErrorItem[]>([]);
  // 导入列定义
  const [importColumns, setImportColumns] = useState<ImportColumn[]>([]);
  // 导入数据列表
  const [importList, setImportList] = useState<Record<string, any>[]>([]);
  // 导入模板信息
  const [importTemplateInfo, setImportTemplateInfo] = useState<ImportTemplateInfo>({
    TemplateCode: "",
    FileId: "",
    TemplateName: "",
    IsAllowOverride: true
  });
  // 导入数据ID
  const [importDataId, setImportDataId] = useState<string | null>(null);

  // 防抖标志
  let uploadFlag = true;
  const { moduleInfo } = props;

  /**
   * 查询导入模板信息
   * 根据模块ID获取对应的导入模板配置
   */
  const querySingleData = useCallback(async () => {
    try {
      setPageLoading(true);
      const { Success, Data } = await http.get<any>(`/api/SmImpTemplate/QueryByModuleId/${props.moduleInfo.moduleId}`);

      if (Success) {
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
  }, [props.moduleInfo.moduleId]);

  // 组件挂载时查询模板信息
  useEffect(() => {
    querySingleData();
  }, [querySingleData]);

  /**
   * 上传前校验
   * 返回false阻止自动上传，改为手动处理上传逻辑
   */
  const beforeUpload = useCallback(() => false, []);

  /**
   * 处理文件上传
   * 上传Excel文件并解析数据
   * @param file 上传的文件对象
   */
  const handleChange = useCallback(
    async (file: any) => {
      if (!uploadFlag) return false;
      uploadFlag = false;

      // 文件类型校验
      const isExcel =
        file.file.type === "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" ||
        file.file.type === "application/vnd.ms-excel";
      if (!isExcel) {
        message.error("请选择正确的Excel文件(.xlsx或.xls)!");
        uploadFlag = true;
        return;
      }

      try {
        message.loading("上传中..", 0);
        const formData = new FormData();
        formData.append("file", file.file);
        formData.append("fileName", file.file.name);

        const { Success, Data, Message } = await uploadFile(`/api/Common/ImportExcel/${props.moduleInfo.moduleCode}`, formData);

        if (Success && Data) {
          // 处理成功数据
          const processedList = Data.ImportList.map((item: Record<string, any>, index: number) => ({
            ...item,
            Key: index + 1
          }));

          const columns = [
            { title: "序号", dataIndex: "Key", key: "Key" },
            ...Data.ImportColumns.map((col: string, j: number) => ({
              title: Data.ImportColumnNames[j],
              dataIndex: col,
              key: `Key_${j}`
            }))
          ];

          setStepsCurrent(1);
          setImportList(processedList);
          setImportColumns(columns);
          setImportDataId(Data.ImportDataId);
          message.success("上传成功！", 3);
        } else {
          // 处理错误数据
          if (Data && Data.errorList && Data.errorList.length > 0) {
            const processedErrors = Data.errorList.map((item: Omit<ErrorItem, "Key">, index: number) => ({
              ...item,
              Key: index + 1
            }));

            setStepsCurrent(1);
            setErrorList(processedErrors);
          }
          message.error(Message || "上传失败", 3);
        }
      } catch (error) {
        console.error("上传失败:", error);
        message.error("上传过程中发生错误");
      } finally {
        message.destroy();
        uploadFlag = true;
      }
    },
    [props.moduleInfo.moduleCode]
  );

  /**
   * 数据转换处理
   * 将上传的Excel数据转换为系统数据并保存
   * @param type 处理类型 (append: 追加导入 / override: 覆盖导入)
   */
  const okTransferData = useCallback(
    async (type: "append" | "override") => {
      if (!importDataId) {
        message.error("导入数据ID不存在，请重新上传");
        return;
      }

      try {
        message.loading("数据转换中..", 0);
        const { Success, Message } = await http.post<any>(`/api/Common/TransferExcelData/${props.moduleInfo.moduleCode}`, {
          type,
          importDataId,
          importTemplateCode: importTemplateInfo?.TemplateCode,
          masterId: props.moduleInfo.masterId ?? null
        });

        if (Success) {
          props.onReload();
          setStepsCurrent(2);
          message.success(Message || "导入成功", 3);
        } else {
          message.error(Message || "导入失败", 3);
        }
      } catch (error) {
        console.error("数据转换失败:", error);
        message.error("数据转换过程中发生错误");
      } finally {
        message.destroy();
      }
    },
    [importDataId, importTemplateInfo, props]
  );

  /**
   * 下载模板文件
   * @param fileId 文件ID
   * @param templateName 模板名称
   */
  const onDownload = useCallback((fileId: string, templateName: string) => {
    if (!fileId) {
      message.error("请先在【导入管理】维护导入模板！", 3);
      return;
    }
    downloadFile(fileId, templateName);
  }, []);

  /**
   * 重置上传状态
   * 返回到上传步骤并清空数据
   */
  const resetUpload = useCallback(() => {
    setStepsCurrent(0);
    setImportDataId(null);
    setErrorList([]);
    setImportList([]);
  }, []);

  /**
   * 渲染上传步骤内容
   */
  const renderUploadStep = useCallback(() => {
    return (
      <Form labelCol={{ span: 8 }} wrapperCol={{ span: 18 }} style={{ marginTop: 20 }}>
        <Row gutter={24} justify={"center"}>
          <Col span={24}>
            <FormItem label="Excel文件：">
              <Space>
                <Upload
                  accept=".xlsx,.xls"
                  action={""}
                  showUploadList={false}
                  beforeUpload={beforeUpload}
                  onChange={handleChange}
                >
                  <Button type="primary" icon={<Icon name="UploadOutlined" />}>
                    点击上传Excel文件
                  </Button>
                </Upload>
              </Space>
            </FormItem>
          </Col>
          <Col span={24}>
            <FormItem
              labelCol={{
                xs: { span: 6 },
                sm: { span: 6 },
                md: { span: 6 }
              }}
              wrapperCol={{
                xs: { span: 16 },
                sm: { span: 16 },
                md: { span: 16 }
              }}
              label="导入步骤："
            >
              <div style={{ marginTop: 5 }}>
                1、下载导入模板：
                <a onClick={() => onDownload(importTemplateInfo.FileId, importTemplateInfo.TemplateName)} key="link">
                  {moduleInfo.moduleName} 导入模板
                </a>
              </div>
              <div style={{ marginTop: 10 }}>2、根据模板中的格式填写内容，不可以调整列的先后顺序。</div>
              <div style={{ marginTop: 10 }}>3、点击"选择Excel文件"执行上传操作。</div>
            </FormItem>
          </Col>
          <Col span={24}>
            <FormItem
              labelCol={{
                xs: { span: 6 },
                sm: { span: 6 },
                md: { span: 6 }
              }}
              wrapperCol={{
                xs: { span: 16 },
                sm: { span: 16 },
                md: { span: 16 }
              }}
              label="注意事项："
            >
              <div style={{ marginTop: 5 }}>1、后缀名必须为xlsx或xls。</div>
              <div style={{ marginTop: 10 }}>2、数据请勿放在合并的单元格中。</div>
              <div style={{ marginTop: 10 }}>
                3、第一行红色字体的为必填栏位，同时注意特殊字段的格式是否正确，例如：日期类型，数字类型等。
              </div>
              <div style={{ marginTop: 10 }}>4、不可以调整Excel模板中列的顺序。</div>
              <div style={{ marginTop: 10 }}>5、不可以修改导入模板中的工作簿(Sheet)名称。</div>
              <div style={{ marginTop: 10 }}>
                6、导入数据时，系统会将第一行的内容作为标题行，因此导入的内容请从第2行开始填写。
              </div>
            </FormItem>
          </Col>
        </Row>
      </Form>
    );
  }, [beforeUpload, handleChange, importTemplateInfo, moduleInfo.moduleName, onDownload]);

  /**
   * 渲染错误预览步骤内容
   */
  const renderErrorPreview = useCallback(() => {
    return (
      <>
        <Result
          style={{ padding: 20 }}
          status="error"
          title="读取失败"
          subTitle={errorList.length + "条错误信息"}
          extra={[
            <Button type="primary" key="console" icon={<Icon name="ArrowUpOutlined" />} onClick={resetUpload}>
              返回上一页
            </Button>
          ]}
        />
        <Table
          columns={[
            {
              title: "序号",
              dataIndex: "Key",
              key: "Key"
            },
            {
              title: "Sheet名",
              dataIndex: "SheetName",
              key: "SheetName"
            },
            {
              title: "错误信息",
              dataIndex: "ErrorName",
              key: "ErrorName"
            }
          ]}
          dataSource={errorList}
        />
      </>
    );
  }, [errorList, resetUpload]);

  /**
   * 渲染数据预览步骤内容
   */
  const renderDataPreview = useCallback(() => {
    return (
      <>
        <Result
          style={{ padding: 20 }}
          status="success"
          title="读取成功"
          subTitle={`本次从Excel共读取数据：${importList.length}笔，以下只显示了部分数据供用户预览`}
          extra={[
            <Button type="primary" key="append" icon={<Icon name="PlusOutlined" />} onClick={() => okTransferData("append")}>
              追加导入
            </Button>,
            importTemplateInfo?.IsAllowOverride && (
              <Button key="override" danger icon={<Icon name="ImportOutlined" />} onClick={() => okTransferData("override")}>
                覆盖导入
              </Button>
            ),
            <Button type="primary" key="console" icon={<Icon name="ArrowUpOutlined" />} onClick={resetUpload}>
              返回上一页
            </Button>
          ]}
        />
        <Table columns={importColumns} dataSource={importList} />
      </>
    );
  }, [importColumns, importList, importTemplateInfo, okTransferData, resetUpload]);

  /**
   * 渲染导入成功步骤内容
   */
  const renderImportSuccess = useCallback(() => {
    return (
      <Result
        style={{ padding: 20 }}
        status="success"
        title="导入成功"
        extra={[
          <Button type="primary" key="console" icon={<Icon name="RollbackOutlined" />} onClick={resetUpload}>
            返回
          </Button>
        ]}
      />
    );
  }, [resetUpload]);

  /**
   * 渲染步骤内容
   */
  const renderStepContent = useCallback(() => {
    if (stepsCurrent === 0) {
      return renderUploadStep();
    }

    if (stepsCurrent === 1) {
      if (errorList.length > 0) {
        return renderErrorPreview();
      }
      if (importList.length > 0) {
        return renderDataPreview();
      }
    }

    if (stepsCurrent === 2) {
      return renderImportSuccess();
    }

    return null;
  }, [
    stepsCurrent,
    errorList.length,
    importList.length,
    renderUploadStep,
    renderErrorPreview,
    renderDataPreview,
    renderImportSuccess
  ]);

  // 渲染组件
  return (
    <>
      {!pageLoading ? (
        <>
          <Steps type="navigation" current={stepsCurrent} size="small" className="site-navigation-steps">
            <Step status="process" title="上传Excel" />
            <Step status="process" title="数据预览" />
            <Step status="process" title="导入数据" />
          </Steps>
          {renderStepContent()}
        </>
      ) : (
        <>
          <Skeleton />
          <Skeleton />
        </>
      )}
    </>
  );
};

// 使用React.memo优化组件性能，避免不必要的重渲染
export default React.memo(UploadExcel);
