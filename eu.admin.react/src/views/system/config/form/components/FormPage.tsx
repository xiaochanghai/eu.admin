import { Tabs, Card, Form, Flex, Tag, Button } from "antd";
import { Icon } from "@/components/Icon";
import Layout from "@/components/Elements/Index";
import React from "react";
import { useDrag, useDrop } from "react-dnd";
import { HTML5Backend } from "react-dnd-html5-backend";
import { DndProvider } from "react-dnd";
import http from "@/api";
const { TabPane } = Tabs;

interface FieldSetCenterProps {
  moduleCode: any;
  fieldList: any[];
  currentField: any;
  onDataChange: (ang: any[]) => void; //数据返回出去
  onSelect: (field: string) => void; //当前选中字段
  onPlus: (field: any) => void; //当前选中字段
}
const FieldSetCenter = ({ fieldList, currentField, onSelect, onDataChange, onPlus, moduleCode }: FieldSetCenterProps) => {
  const DragFormHideItem = ({ id, text, index, moveItem, field }: any) => {
    const ref = React.useRef(null);

    const [{ isDragging }, drag] = useDrag(() => ({
      type: "item",
      item: { id, index },
      collect: monitor => ({
        isDragging: !!monitor.isDragging()
      })
    }));

    const [, drop] = useDrop(() => ({
      accept: "item",
      hover: (item: any) => {
        if (item.index !== index) {
          moveItem(item.index, index);
          item.index = index;
        }
      }
    }));

    drag(drop(ref));

    return (
      <div
        ref={ref}
        style={{
          // width: 120,
          opacity: isDragging ? 0.5 : 1,
          cursor: "move"
          // padding: "10px",
          // border: "1px solid #ccc",
          // margin: "5px"
        }}
      >
        {/* {text} */}
        <Tag
          className="main-hide-field-tag"
          style={{
            width: 100,
            textAlign: "center",
            border: currentField?.ID === field?.ID ? "1px solid #3b82f680" : "",
            background: currentField?.ID === field?.ID ? "#F8FBFF" : ""
            // borderRadius: currentField?.ID === item?.ID ? 10 : 0,
            // width: (item.GridSpan != null ? item?.GridSpan : 50) + "%"
          }}
          onClick={event => {
            event.stopPropagation();
            // 处理按钮的点击事件
            onSelect(field);
          }}
        >
          {text}
          <span
            className="plus"
            onClick={event => {
              event.stopPropagation();
              onPlus({
                ...field,
                HideInForm: false
              });
            }}
          >
            <Icon className={currentField?.ID === field?.ID ? "icon active" : "icon"} name="PlusCircleFilled" />
          </span>
        </Tag>
      </div>
    );
  };

  const moveFormHideItem = (fromIndex: number, toIndex: number) => {
    let updatedItems = [...fieldList.filter(f => f.HideInForm != false)];
    const [movedItem] = updatedItems.splice(fromIndex, 1);
    updatedItems.splice(toIndex, 0, movedItem);
    // debugger;
    let items = [...fieldList.filter(f => f.HideInForm == false), ...updatedItems];
    saveFormColumnTaxisNo(items);
    onDataChange(items);
  };

  const DragFormItem = ({ id, index, moveItem, field }: any) => {
    const ref = React.useRef(null);

    const [{ isDragging }, drag] = useDrag(() => ({
      type: "item",
      item: { id, index },
      collect: monitor => ({
        isDragging: !!monitor.isDragging()
      })
    }));

    const [, drop] = useDrop(() => ({
      accept: "item",
      hover: (item: any) => {
        if (item.index !== index) {
          moveItem(item.index, index);
          item.index = index;
        }
      }
    }));

    drag(drop(ref));

    return (
      <div
        ref={ref}
        className="main-field"
        style={{
          border: currentField?.ID === field?.ID ? "1px solid #3b82f680" : "",
          background: currentField?.ID === field?.ID ? "#F8FBFF" : "",
          // borderRadius: currentField?.ID === item?.ID ? 10 : 0,
          width: (field.GridSpan != null ? field?.GridSpan : 50) + "%",
          opacity: isDragging ? 0.5 : 1,
          cursor: "move"
        }}
        key={index}
        onClick={() => onSelect(field)}
      >
        <Layout field={field} />
      </div>
    );
  };

  const moveFormItem = (fromIndex: number, toIndex: number) => {
    let updatedItems = [...fieldList.filter(f => f.HideInForm === false)];
    const [movedItem] = updatedItems.splice(fromIndex, 1);
    updatedItems.splice(toIndex, 0, movedItem);

    let items = [...fieldList.filter(f => f.HideInForm != false), ...updatedItems];
    saveFormColumnTaxisNo(items);
    onDataChange(items);
  };
  const saveFormColumnTaxisNo = async (columns: any) =>
    await http.put<any>(`/api/SmModule/UpdateFormColumnTaxisNo/${moduleCode}`, columns);

  return (
    <div style={{ backgroundColor: "#fff" }}>
      <Tabs
        defaultActiveKey={"panel_table"}
        tabBarExtraContent={{
          right: (
            <Button type="primary" icon={<Icon name="PlusOutlined" />}>
              新增
            </Button>
          )
        }}
      >
        <TabPane key="panel_table" tab="表格栏位" icon={<Icon name="TableOutlined" />}>
          <Form
            labelCol={{
              xs: { span: 8 },
              sm: { span: 8 },
              md: { span: 8 }
            }}
            wrapperCol={{
              xs: { span: 16 },
              sm: { span: 16 },
              md: { span: 16 }
            }}
            labelWrap
          >
            <Card size="small" title="可编辑栏位">
              <DndProvider backend={HTML5Backend}>
                <div style={{ padding: "20px" }}>
                  <Flex wrap>
                    {fieldList
                      .filter(f => f.HideInForm === false)
                      .map((item, index) => (
                        <DragFormItem key={item.ID} id={item.ID} index={index} field={item} moveItem={moveFormItem} />
                      ))}
                  </Flex>
                </div>
              </DndProvider>
            </Card>
            <div style={{ height: 20 }}></div>
            <Card size="small" title="隐藏栏位">
              <DndProvider backend={HTML5Backend}>
                <div style={{ padding: "20px" }}>
                  <Flex wrap gap="small">
                    {fieldList
                      .filter(f => f.HideInForm != false)
                      .map((item, index) => (
                        <DragFormHideItem
                          key={item.ID}
                          id={item.ID}
                          text={item.FormTitle || item.DataIndex}
                          index={index}
                          field={item}
                          moveItem={moveFormHideItem}
                        />
                      ))}
                  </Flex>
                </div>
              </DndProvider>
            </Card>
          </Form>
        </TabPane>
        <TabPane key="panel_form" tab="表单栏位" icon={<Icon name="MenuOutlined" />}>
          1111
        </TabPane>
      </Tabs>
    </div>
  );
};

export default FieldSetCenter;
