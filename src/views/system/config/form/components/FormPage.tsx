import { Tabs, Card, Form, Flex, Tag } from "antd";
import { Icon } from "@/components/Icon";
import Layout from "@/components/Elements/Layout";
// import { Mode } from "./dsl/base";
import React from "react";
import { useDrag, useDrop } from "react-dnd";
import { HTML5Backend } from "react-dnd-html5-backend";
import { DndProvider } from "react-dnd";
import http from "@/api";
const { TabPane } = Tabs;

interface FieldSetCenterProps {
  // mode: Mode;
  fieldList: any[];
  currentField: any;
  onDataChange: (ang: any[]) => void; //数据返回出去
  onSelect: (field: string) => void; //当前选中字段
}
const FieldSetCenter = ({ fieldList, currentField, onSelect, onDataChange }: FieldSetCenterProps) => {
  //删除添加的布局字段
  // const handleDelete = (e, id) => {
  //   e.stopPropagation();
  //   const newFieldList = [...fieldList];
  //   const curIndex = newFieldList.findIndex((item) => item.id === id);
  //   if (curIndex !== -1) {
  //     let deleteItem = newFieldList.splice(curIndex, 1);
  //     if (deleteItem?.[0]?.id === currentField?.id) {
  //       handleChooseField({});
  //     }
  //     onFieldChange(newFieldList);
  //   }
  // };
  const DragItem = ({ id, text, index, moveItem, field }: any) => {
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
          style={{ width: 100, textAlign: "center" }}
          onClick={event => {
            event.stopPropagation();
            // 处理按钮的点击事件
            onSelect(field);
          }}
        >
          {text}
          <span className="plus">
            <Icon className="icon" name="PlusCircleFilled" />
          </span>
        </Tag>
      </div>
    );
  };

  const moveItem = (fromIndex: number, toIndex: number) => {
    let updatedItems = [...fieldList.filter(f => f.HideInForm != false)];
    const [movedItem] = updatedItems.splice(fromIndex, 1);
    updatedItems.splice(toIndex, 0, movedItem);
    // debugger;
    let items = [...fieldList.filter(f => f.HideInForm == false), ...updatedItems];
    saveFormColumnTaxisNo(items);
    onDataChange(items);
  };
  const saveFormColumnTaxisNo = async (columns: any) =>
    await http.put<any>(`/api/SmModule/UpdateFormColumnTaxisNo/SD_SALES_ORDER_MNG`, columns);
  return (
    // <ReactSortable
    //   list={fieldList || []}
    //   animation={200}
    //   group={{ name: "sort-field" }}
    //   setList={(list) =>
    //     onFieldChange(
    //       list.map((item) => ({
    //         ...item,
    //         id: item?.id ? item?.id : new Date().getTime(),
    //       }))
    //     )
    //   }
    //   sort={true}
    //   forceFallback={true}
    //   className="field-center"
    // >
    <div style={{ backgroundColor: "#fff" }}>
      <Tabs defaultActiveKey={"panel_table"}>
        <TabPane
          key={"panel_table"}
          tab="表格栏位"
          // icon={<t.icon></t.icon>}
          // style={{ padding: "2px" }}
        >
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
            <Card size="small" title={<span>可编辑栏位</span>}>
              <Flex wrap="wrap">
                {fieldList.filter(f => f.HideInForm === false).length === 0 ? (
                  <div className="main-tooltip">请选择左侧的字段</div>
                ) : (
                  fieldList
                    .filter(f => f.HideInForm === false)
                    .map((item, index) => {
                      return (
                        <div
                          className="main-field"
                          style={{
                            border: currentField?.ID === item?.ID ? "1px solid #3b82f680" : "",
                            background: currentField?.ID === item?.ID ? "#F8FBFF" : "",
                            // borderRadius: currentField?.ID === item?.ID ? 10 : 0,
                            width: (item.GridSpan != null ? item?.GridSpan : 50) + "%"
                          }}
                          key={index}
                          onClick={() => onSelect(item)}
                        >
                          <Layout field={item} />
                        </div>
                      );
                    })
                )}
              </Flex>
            </Card>
            <div style={{ height: 20 }}></div>
            <Card size="small" title={<span>隐藏栏位</span>}>
              <DndProvider backend={HTML5Backend}>
                <div style={{ padding: "20px" }}>
                  <Flex wrap gap="small">
                    {fieldList
                      .filter(f => f.HideInForm != false)
                      .map((item, index) => (
                        <DragItem
                          key={item.ID}
                          id={item.ID}
                          text={item.FormTitle || item.DataIndex}
                          index={index}
                          field={item}
                          moveItem={moveItem}
                        />
                      ))}
                  </Flex>
                </div>
              </DndProvider>
            </Card>
          </Form>
        </TabPane>
        <TabPane
          key={"panel_form"}
          tab="表单栏位"
          // icon={<t.icon></t.icon>}
          // style={{ padding: "2px" }}
        >
          1111
        </TabPane>
      </Tabs>
    </div>
    // </ReactSortable>
  );
};

export default FieldSetCenter;
