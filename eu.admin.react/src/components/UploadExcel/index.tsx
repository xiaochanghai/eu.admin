import { useEffect, useState } from "react";
import { Upload, Form, Row, Col, Steps, Button, Space, Result, Table } from "antd";
import { message } from "@/hooks/useMessage";
import { downloadFile } from "@/utils";
import http from "@/api";
import { uploadFile } from "@/api/modules/module";
import { Icon } from "@/components";

const { Step } = Steps;
const FormItem = Form.Item;

let flag = true;
const UploadExcel = (props: any) => {
  // const [Id, setId] = useState(null);
  const [stepsCurrent, setStepsCurrent] = useState(0);
  const [errorList, setErrorList] = useState([]);
  const [importColumns, setImportColumns] = useState<any>([]);
  const [importList, setImportList] = useState([]);
  const [importTemplateInfo, setImportTemplateInfo] = useState<any>({});
  const [importDataId, setImportDataId] = useState(null);
  const {
    moduleInfo,
    // onCancel,
    onReload,
    moduleInfo: { masterId, moduleCode, moduleId }
  } = props;
  const querySingleData = async () => {
    let { Success, Data } = await http.get<any>(`/api/SmImpTemplate/QueryByModuleId/${moduleId}`);
    if (Success) setImportTemplateInfo(Data);
  };
  useEffect(() => {
    querySingleData();
  }, []);

  const beforeUpload = () => {
    return false;
  };
  const handleChange = async (file: any) => {
    if (flag) flag = false;
    else return false;

    const isJpgOrPng = file.file.type === "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
    if (!isJpgOrPng) {
      message.error("请选择正确的Excel文件!");
      return;
    }

    //附件上传
    message.loading("上传中..", 0);
    const formData = new FormData();
    formData.append("file", file.file);
    // formData.append("moduleCode", moduleCode);
    formData.append("fileName", file.file.name);

    let { Success, Data, Message } = await uploadFile(`/api/Common/ImportExcel/${moduleCode}`, formData);

    // let { Success, Message } = await http.post<any>("/api/File/" + record.ID);

    message.destroy();
    flag = true;
    if (Success) {
      let importList = Data.ImportList;
      for (let index = 0; index < importList.length; index++) importList[index].Key = index + 1;
      let importColumns = [
        {
          title: "序号",
          dataIndex: "Key",
          key: "Key"
        }
      ];
      let importColumns1 = Data.ImportColumns;
      let importColumnNames = Data.ImportColumnNames;
      if (importColumns1 && importColumns1.length > 0) {
        for (let j = 0; j < importColumns1.length; j++)
          importColumns.push({
            title: importColumnNames[j],
            dataIndex: importColumns1[j],
            key: "Key_" + j
          });
      }
      setStepsCurrent(1);
      setImportList(importList);
      setImportColumns(importColumns);
      setImportDataId(Data.ImportDataId);
      // onCancel();
      message.success("上传成功！", 3);
    } else {
      let errorList = Data.errorList;
      for (let index = 0; index < errorList.length; index++) errorList[index].Key = index + 1;
      if (errorList.length > 0) {
        setStepsCurrent(1);
        setErrorList(errorList);
      }

      message.error(Message, 3);
    }
    // }
  };
  const okTransferData = async (type: any) => {
    message.loading("数据转换中..", 0);
    let { Success, Message } = await http.post<any>(`/api/Common/TransferExcelData/${moduleCode}`, {
      type,
      importDataId,
      importTemplateCode: importTemplateInfo.TemplateCode,
      masterId: masterId ?? null
    });
    message.destroy();
    if (Success) {
      onReload();
      setStepsCurrent(2);
      message.success(Message, 3);
    }
    // else {
    //   message.error(result.message, 3);
    // }
  };

  const onDownload = (fileId: any, templateName: string) => {
    if (!fileId) {
      message.error("请先在【导入管理】维护导入模板！", 3);
      return;
    }
    downloadFile(fileId, templateName);
  };

  return (
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
  );
};

export default UploadExcel;
