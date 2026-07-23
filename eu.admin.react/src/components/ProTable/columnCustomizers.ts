import type { ProColumns } from "@ant-design/pro-components";

export type ColumnCustomizer<T = any> = (originalColumn: ProColumns<T>) => ProColumns<T>;

export type ColumnCustomizerMap<T = any> = Record<string, ColumnCustomizer<T>>;

const getDataIndexKey = (dataIndex: ProColumns<any>["dataIndex"]): string => {
  return Array.isArray(dataIndex) ? dataIndex.join(".") : String(dataIndex ?? "");
};

const getColumnCustomizer = <T>(
  columnCustomizers: ColumnCustomizerMap<T>,
  dataIndex: ProColumns<T>["dataIndex"]
): ColumnCustomizer<T> | undefined => {
  const key = getDataIndexKey(dataIndex);
  return Object.prototype.hasOwnProperty.call(columnCustomizers, key)
    ? columnCustomizers[key]
    : undefined;
};

const replaceColumnByDataIndex = <T>(
  columns: ProColumns<T>[],
  dataIndexKey: string,
  replacement: ProColumns<T>
): { columns: ProColumns<T>[]; replaced: boolean } => {
  let replaced = false;
  const nextColumns = columns.map(column => {
    if (getDataIndexKey(column.dataIndex) === dataIndexKey) {
      replaced = true;
      return replacement;
    }

    const children = column.children as ProColumns<T>[] | undefined;
    if (!children?.length) return column;

    const nestedResult = replaceColumnByDataIndex(children, dataIndexKey, replacement);
    if (!nestedResult.replaced) return column;

    replaced = true;
    return { ...column, children: nestedResult.columns };
  });

  return { columns: nextColumns, replaced };
};

export const applyColumnCustomizers = <T>(
  columns: ProColumns<T>[],
  columnCustomizers?: ColumnCustomizerMap<T>
): ProColumns<T>[] => {
  if (!columnCustomizers) return columns;

  return columns.map(column => {
    let currentColumn = column;
    const children = column.children as ProColumns<T>[] | undefined;

    if (children?.length) {
      const customizedChildren = applyColumnCustomizers(children, columnCustomizers);
      const childrenChanged = customizedChildren.some((child, index) => child !== children[index]);
      if (childrenChanged) currentColumn = { ...column, children: customizedChildren };
    }

    const customize = getColumnCustomizer(columnCustomizers, currentColumn.dataIndex);
    return customize ? customize(currentColumn) : currentColumn;
  });
};

export const buildFinalColumns = <T>(
  columns: ProColumns<T>[],
  actionColumn?: ProColumns<T> | null,
  columnCustomizers?: ColumnCustomizerMap<T>
): ProColumns<T>[] => {
  let columnsWithAction = columns;

  if (actionColumn?.dataIndex) {
    const actionKey = getDataIndexKey(actionColumn.dataIndex);
    const replacementResult = replaceColumnByDataIndex(columns, actionKey, actionColumn);
    columnsWithAction = replacementResult.replaced
      ? replacementResult.columns
      : [...columns, actionColumn];
  }

  return applyColumnCustomizers(columnsWithAction, columnCustomizers);
};
