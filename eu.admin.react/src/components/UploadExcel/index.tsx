import React, { useEffect, useState } from "react";
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
 * Excel上传组件
 * 功能：支持Excel文件上传、数据预览和导入
 * @param {Object} props - 组件属性
 * @param {Object} props.moduleInfo - 模块信息
 * @param {Function} props.onReload - 数据重新加载回调
 */
const UploadExcel = (props: {
  moduleInfo: {
    masterId?: string;
    moduleCode: string;
    moduleId: string;
    moduleName: string;
  };
  onReload: () => void;
  onCancel: () => void;
}) => {
  const [stepsCurrent, setStepsCurrent] = useState(0);
  const [pageLoading, setPageLoading] = useState(true);
  const [errorList, setErrorList] = useState<Array<{ Key: number; SheetName: string; ErrorName: string }>>([]);
  const [importColumns, setImportColumns] = useState<any[]>([]);
  const [importList, setImportList] = useState<any[]>([]);
  const [importTemplateInfo, setImportTemplateInfo] = useState<{
    TemplateCode: string;
    FileId: string;
    TemplateName: string;
    IsAllowOverride: boolean;
  }>({ TemplateCode: "", FileId: "", TemplateName: "", IsAllowOverride: true });
  const [importDataId, setImportDataId] = useState<string | null>(null);

  // 防抖标志
  let uploadFlag = true;
  const { moduleInfo } = props;

  /**
   * 查询导入模板信息
   */
  const querySingleData = async () => {
    try {
      const { Success, Data } = await http.get<any>(`/api/SmImpTemplate/QueryByModuleId/${props.moduleInfo.moduleId}`);
      if (Success) setImportTemplateInfo(Data);
      setPageLoading(false);
    } catch (error) {
      console.error("查询模板失败:", error);
      message.error("获取模板信息失败");
    }
  };

  useEffect(() => {
    querySingleData();
  }, []);

  /**
   * 上传前校验
   */
  const beforeUpload = () => false;

  /**
   * 处理文件上传
   * @param {Object} file - 上传的文件对象
   */
  const handleChange = async (file: any) => {
    if (!uploadFlag) return false;
    uploadFlag = false;

    // 文件类型校验
    const isExcel = file.file.type === "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
    if (!isExcel) {
      message.error("请选择正确的Excel文件!");
      uploadFlag = true;
      return;
    }

    try {
      message.loading("上传中..", 0);
      const formData = new FormData();
      formData.append("file", file.file);
      formData.append("fileName", file.file.name);

      const { Success, Data, Message } = await uploadFile(`/api/Common/ImportExcel/${props.moduleInfo.moduleCode}`, formData);

      if (Success) {
        // 处理成功数据
        const processedList = Data.ImportList.map((item: any, index: number) => ({
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
        const processedErrors = Data.errorList.map((item: any, index: number) => ({
          ...item,
          Key: index + 1
        }));

        if (processedErrors.length > 0) {
          setStepsCurrent(1);
          setErrorList(processedErrors);
        }
        message.error(Message, 3);
      }
    } catch (error) {
      console.error("上传失败:", error);
      message.error("上传过程中发生错误");
    } finally {
      message.destroy();
      uploadFlag = true;
    }
  };

  /**
   * 数据转换处理
   * @param {string} type - 处理类型 (append/override)
   */
  const okTransferData = async (type: string) => {
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
        message.success(Message, 3);
      }
    } catch (error) {
      console.error("数据转换失败:", error);
      message.error("数据转换过程中发生错误");
    } finally {
      message.destroy();
    }
  };

  /**
   * 下载模板文件
   * @param {string} fileId - 文件ID
   * @param {string} templateName - 模板名称
   */
  const onDownload = (fileId: string, templateName: string) => {
    if (!fileId) {
      message.error("请先在【导入管理】维护导入模板！", 3);
      return;
    }
    downloadFile(fileId, templateName);
  };

  return (
    <>
      {!pageLoading ? (
        <>
          <Steps type="navigation" current={stepsCurrent} size="small" className="site-navigation-steps">
            <Step status="process" title="上传Excel" />
            <Step status="process" title="数据预览" />
            <Step status="process" title="导入数据" />
          </Steps>
          {stepsCurrent == 0 ? (
            <Form labelCol={{ span: 8 }} wrapperCol={{ span: 18 }} style={{ marginTop: 20 }}>
              <Row gutter={24} justify={"center"}>
                <Col span={24}>
                  <FormItem label="Excel文件：">
                    <Space>
                      <Upload
                        accept=".xlsx,.xls"
                        // listType="picture-card"
                        // className={styles.uploader}
                        action={""}
                        showUploadList={false}
                        beforeUpload={beforeUpload}
                        // fileList={fileList}
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
                    <div style={{ marginTop: 10 }}>3、点击“选择Excel文件”执行上传操作。</div>
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
          ) : null}
          {stepsCurrent == 1 && errorList.length > 0 ? (
            <>
              <Result
                style={{ padding: 20 }}
                status="error"
                title="读取失败"
                subTitle={errorList.length + "条错误信息"}
                extra={[
                  <Button
                    type="primary"
                    key="console"
                    icon={<Icon name="ArrowUpOutlined" />}
                    onClick={() => {
                      setStepsCurrent(0);
                      setErrorList([]);
                      setImportList([]);
                    }}
                  >
                    返回上一页
                  </Button>
                ]}
              ></Result>
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
          ) : null}
          {stepsCurrent == 1 && importList.length > 0 ? (
            <>
              <Result
                style={{ padding: 20 }}
                status="success"
                title="读取成功"
                subTitle={"本次从Excel共读取数据：" + importList.length + "笔，以下只显示了部分数据供用户预览"}
                extra={[
                  <Button
                    type="primary"
                    key="append"
                    icon={<Icon name="PlusOutlined" />}
                    onClick={() => {
                      okTransferData("append");
                    }}
                  >
                    追加导入
                  </Button>,
                  <>
                    {importTemplateInfo && importTemplateInfo.IsAllowOverride ? (
                      <Button
                        key="override"
                        danger
                        icon={<Icon name="ImportOutlined" />}
                        onClick={() => {
                          okTransferData("override");
                        }}
                      >
                        覆盖导入
                      </Button>
                    ) : null}
                  </>,
                  <Button
                    type="primary"
                    key="console"
                    icon={<Icon name="ArrowUpOutlined" />}
                    onClick={() => {
                      setStepsCurrent(0);
                      setImportDataId(null);
                      setErrorList([]);
                      setImportList([]);
                    }}
                  >
                    返回上一页
                  </Button>
                ]}
              ></Result>
              <Table columns={importColumns} dataSource={importList} />
            </>
          ) : null}
          {stepsCurrent == 2 ? (
            <>
              <Result
                style={{ padding: 20 }}
                status="success"
                title="导入成功"
                // subTitle={'本次从Excel共读取数据：' + importList.length + '笔，以下只显示了部分数据供用户预览'}
                extra={[
                  <Button
                    type="primary"
                    key="console"
                    icon={<Icon name="RollbackOutlined" />}
                    onClick={() => {
                      setStepsCurrent(0);
                      setImportDataId(null);
                      setErrorList([]);
                      setImportList([]);
                    }}
                  >
                    返回
                  </Button>
                ]}
              ></Result>
            </>
          ) : null}
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

export default React.memo(UploadExcel);
