import { Select, Spin } from "antd";
import { useState, useEffect } from "react";
import { getComboGridData } from "@/api/modules/module";
import { SmLovData } from "@/api/interface/index";

let page = 1;
let oldData: any[] = [];
// let searchValue = "";
const ComBoGrid: React.FC<any> = props => {
  const [loading, setLoading] = useState<boolean>(false);
  const [comboValue, setComboValue] = useState<string>("");
  const [dropDownData, setDropDownData] = useState<SmLovData[]>([]);
  let { onChange, code, parentColumn, id, parentId } = props;
  useEffect(() => {
    setComboValue("");
    queryLoadData();
  }, [parentId]);

  /**
   * 下拉数据查询
   * @param {*} sql
   */
  const queryLoadData = async (value = "", isSearch = false) => {
    if (isSearch) {
      page = 1;
      oldData = [];
    }
    // if (!parentId) parentId = props.parentId;

    // searchValue = value; // 记住搜索值，下拉事件调用
    if (value) setLoading(true); // 查询显示加载
    let paramData = {
      current: page,
      pageSize: 1000,
      code: code ?? id,
      key: value,
      parentColumn,
      parentId
    };

    // 联动上级数据
    // let newParentId;
    // let oldParentId;
    // if (parentId) newParentId = Object.values(parentId)[0];
    // else if (this.state.parentId) oldParentId = Object.values(this.state.parentId)[0];

    // if (newParentId != oldParentId) {
    //   page = 1;
    //   oldData = [];
    //   this.setState({ comboValue: "", loading: false, dropDownData: oldData });
    // }

    let { Data, Success } = await getComboGridData(paramData);
    setDropDownData(Data);

    if (Success && Data.length > 0) setDropDownData([...oldData, ...Data]);
    else setDropDownData(oldData);
    setLoading(false);
  };

  return (
    <Select
      allowClear
      showSearch={true}
      filterOption={false}
      value={comboValue}
      notFoundContent={loading ? <Spin size="small" /> : null}
      style={{ width: "100%" }}
      onSearch={value => {
        queryLoadData(value, true);
      }}
      {...props}
      onChange={(value, Option) => {
        let r = null;
        if (dropDownData && dropDownData.length > 0)
          r = dropDownData.filter(function (s) {
            return s.value === value; // 注意：IE9以下的版本没有trim()方法
          });
        setComboValue(value);
        if (onChange) onChange(value, Option, r);
      }}
      onClear={() => {
        queryLoadData();
      }}
      options={dropDownData}
    />
  );
};

export default ComBoGrid;
