import React, { useState, useEffect } from "react";
// import FieldSelect from "./FieldSelect";
import SiderSetting from "./SiderSetting";
import FormPage from "./FormPage";
import { Mode } from "./dsl/base";
import { FormSetDiv } from "./style";
import { Button, Card, Row, Col, Space, Skeleton, Descriptions, DescriptionsProps } from "antd";
import http from "@/api";
import { Icon } from "@/components";
import { message } from "@/hooks/useMessage";
import { getModuleInfoById } from "@/api/modules/module";

interface FormDesignProps {
  /** 当前配置的 SmModule 主键。 */
  ModuleId: string;
  /** 旧模块配置入口传入的查看标识，当前组件保留以兼容调用方。 */
  IsView?: boolean;
  /** 旧模块配置入口的页面切换回调。 */
  changePage?: (...args: any[]) => void;
  /** 旧模块配置入口的数据刷新回调。 */
  onReload?: () => void;
  /** 在流程设置页内嵌渲染，仅显示表单栏位并固定使用表单模式。 */
  embedded?: boolean;
}

const FormDesign: React.FC<FormDesignProps> = props => {
  const { ModuleId, changePage, embedded = false } = props;
  let [currentField, setCurrentField] = useState<any>(null);
  let [moduleCode, setModuleCode] = useState<any>(null);
  let [moduleName, setModuleName] = useState<any>(null);
  let [columns, setColumns] = useState<any[]>([]);
  let [mode, setMode] = useState<Mode>(embedded ? Mode.form : Mode.list);

  const queryFormColumn = async () => {
    let { Data } = await http.get<any>(`/api/SmModule/FormColumn/${ModuleId}`);
    setColumns(Data);
  };
  const querySingleData = async () => {
    let { Data, Success } = await getModuleInfoById(ModuleId);
    if (Success && Data) {
      setModuleCode(Data.ModuleCode);
      setModuleName(Data.ModuleName);
    }
  };
  useEffect(() => {
    if (ModuleId) querySingleData();
    if (ModuleId) queryFormColumn();
  }, []);

  let currModel: any = {};

  const save = async () => {
    let { Success, Message } = await http.put<any>(`/api/SmModule/UpdateColumn/${moduleCode}/${mode}`, currentField);
    if (Success) message.success(Message);
  };

  const descriptionItems: DescriptionsProps["items"] = [
    {
      key: "1",
      label: "模块代码",
      children: moduleCode
    },
    {
      key: "2",
      label: "模块名称",
      children: moduleName
    }
  ];

  return (
    <>
      {/* <div style={{ height: 10 }}></div> */}
      {moduleCode == null ? (
        <Card>
          <Skeleton active />
          <Skeleton active />
          <Skeleton active />
          <Skeleton active />
        </Card>
      ) : (
        <>
          {!embedded && (
            <>
              <Space style={{ display: "flex", justifyContent: "flex-end" }}>
                <Button type="default" onClick={() => changePage?.("FormIndex")} icon={<Icon name="RollbackOutlined" />}></Button>
              </Space>
              <div style={{ height: 10 }}></div>
            </>
          )}
          <Card>
            <Descriptions title="表单配置" items={descriptionItems} />

            <div style={{ height: 10 }}></div>
            <FormSetDiv className={"bg-white"}>
              <div className="fieldSet-main">
                <Row className={"bg-white"}>
                  <Col span={3}> </Col>
                  <Col span={15}> </Col>
                  <Col span={6}>
                    {currentField && (
                      <Space style={{ display: "flex", justifyContent: "flex-end" }}>
                        <Button onClick={save} icon={<Icon name="SaveOutlined" />}>
                          保存
                        </Button>
                      </Space>
                    )}
                  </Col>
                </Row>
                <Row className="fieldSet-main-content">
                  {/* 模块选择 */}
                  {/* <Col span={3} className="fieldSet-main-content-left">
            <FieldSelect
              key={currModel.type + "_fieldSelect"}
              fields={columns}
              currentField={currentField}
              onDataChange={fields => {
                setColumns(fields);
              }}
              onSelect={field => {
                setCurrentField(field);
              }}
            />
          </Col> */}
                  {/* 表单预览 */}
                  <Col span={18} className="fieldSet-main-content-center">
                    <FormPage
                      moduleCode={moduleCode}
                      fieldList={columns}
                      formOnly={embedded}
                      // fieldList={columns.sort((a, b) => a.FormTaxisNo - b.FormTaxisNo)}
                      currentField={currentField}
                      // mode={Mode.form}
                      onDataChange={fields => {
                        setColumns(fields);
                      }}
                      onPlus={field => {
                        setColumns(columns.map(f => (f.ID === field.ID ? field : f)));
                      }}
                      onSelect={field => {
                        setCurrentField(field);
                      }}
                      onSetMode={(mode: Mode) => {
                        setMode(mode);
                        setCurrentField(null);
                      }}
                      onReload={() => {
                        querySingleData();
                        queryFormColumn();
                      }}
                    />
                  </Col>
                  {/* form表单设置 */}
                  <Col span={6} className="fieldSet-main-content-right">
                    {currentField && (
                      <>
                        <SiderSetting
                          mode={mode}
                          form={currModel}
                          field={currentField}
                          onDataChange={data => {
                            setCurrentField(data);
                            setColumns(columns.map(f => (f.ID === data.ID ? data : f)));
                          }}
                        />
                      </>
                    )}
                  </Col>
                </Row>
              </div>
            </FormSetDiv>
          </Card>
        </>
      )}
    </>
  );
};

export default FormDesign;
