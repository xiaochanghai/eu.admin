import { Select, Spin, message } from "antd";
import { useState, useEffect, useCallback } from "react";
import { getComboGridData } from "@/api/modules/module";
import { SmLovData } from "@/typings";

interface ComBoGridProps {
  value?: string | null;
  onChange?: (value: string | null, option?: any, record?: SmLovData[] | null) => void;
  code?: string;
  id?: string;
  parentColumn?: string;
  parentId?: string | number | null;
  pageSize?: number;
  [key: string]: any;
}

const ComBoGrid: React.FC<ComBoGridProps> = props => {
  const { onChange, code, parentColumn, id, parentId, pageSize = 1000, value, ...restProps } = props;
  const [loading, setLoading] = useState<boolean>(false);
  const [comboValue, setComboValue] = useState<string | null | undefined>("");
  const [dropDownData, setDropDownData] = useState<SmLovData[]>([]);
  const [currentPage, setCurrentPage] = useState<number>(1);
  const [cachedData, setCachedData] = useState<SmLovData[]>([]);

  // 同步外部 value
  useEffect(() => {
    if (value !== undefined) {
      setComboValue(value);
    }
  }, [value]);

  // 当 parentId 变化时重置组件状态
  useEffect(() => {
    setComboValue(value ?? null);
    setCurrentPage(1);
    setCachedData([]);
    queryLoadData("", false, 1);
  }, [parentId]);

  /**
   * 下拉数据查询
   * @param searchValue - 搜索关键字
   * @param isSearch - 是否为搜索操作
   * @param targetPage - 目标页码
   */
  const queryLoadData = useCallback(
    async (searchValue = "", isSearch = false, targetPage?: number) => {
      try {
        const pageToLoad = targetPage ?? currentPage;

        // 搜索时重置状态
        if (isSearch) {
          setCurrentPage(1);
          setCachedData([]);
        }

        // 显示加载状态
        if (searchValue) {
          setLoading(true);
        }

        const paramData = {
          current: isSearch ? 1 : pageToLoad,
          pageSize,
          code: code ?? id,
          key: searchValue,
          parentColumn,
          parentId
        };

        const { Data, Success } = await getComboGridData(paramData);
        if (Success && Data && Data.length > 0) {
          // 搜索时使用新数据，否则追加数据
          const newData = isSearch || pageToLoad === 1 ? Data : [...cachedData, ...Data];
          setCachedData(newData);
          setDropDownData(newData);

          // 更新页码（用于滚动加载更多）
          if (!isSearch) {
            setCurrentPage(prev => prev + 1);
          }
        } else if (isSearch || targetPage === 1) {
          // 搜索无结果时清空
          setCachedData([]);
          setDropDownData([]);
        } else {
          // 保持原有数据
          setDropDownData(cachedData);
        }
      } catch (error) {
        console.error("ComBoGrid 数据加载失败:", error);
        message.error("数据加载失败，请重试");
        setDropDownData(cachedData);
      } finally {
        setLoading(false);
      }
    },
    [currentPage, cachedData, pageSize, code, id, parentColumn, parentId]
  );

  /**
   * 处理值变化
   */
  const handleChange = useCallback(
    (newValue: string, option: any) => {
      let record: SmLovData[] | null = null;

      if (newValue && dropDownData.length > 0) {
        record = dropDownData.filter(item => item.value === newValue);
      }

      setComboValue(newValue);

      if (onChange) {
        onChange(newValue, option, record);
      }
    },
    [dropDownData, onChange]
  );

  /**
   * 处理搜索
   */
  const handleSearch = useCallback(
    (searchValue: string) => {
      queryLoadData(searchValue, true);
    },
    [queryLoadData]
  );

  /**
   * 处理清空
   */
  const handleClear = useCallback(() => {
    setCurrentPage(1);
    setCachedData([]);
    queryLoadData("", false, 1);
  }, [queryLoadData]);

  return (
    <Select
      allowClear
      showSearch={{ filterOption: false, onSearch: handleSearch }}
      value={comboValue}
      notFoundContent={loading ? <Spin size="small" /> : null}
      style={{ width: "100%" }}
      onChange={handleChange}
      onClear={handleClear}
      options={dropDownData}
      {...restProps}
    />
  );
};

export default ComBoGrid;
