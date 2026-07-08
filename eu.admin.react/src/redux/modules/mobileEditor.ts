import { createSlice, PayloadAction } from "@reduxjs/toolkit";
import { v1 as uuid } from "uuid";
import { traverse } from "@/utils";

/** 移动端组件节点 */
export interface MobileNodeSchema {
  id: string;
  type: string;
  displayName?: string;
  props: Record<string, any>;
  children: MobileNodeSchema[];
}

/** 编辑器状态 */
export interface MobileEditorState extends MobileNodeSchema {
  focusId?: string;
  /** 是否处于 Item 编辑模式 */
  editingItem?: boolean;
  /** 当前正在编辑 Item 的 List 节点 ID */
  listNodeId?: string;
}

const initialState: MobileEditorState = {
  id: "root",
  type: "page",
  props: {},
  children: []
};

const mobileEditorSlice = createSlice({
  name: "mobileEditor",
  initialState,
  reducers: {
    /** 追加组件到根节点 */
    append: (state, action: PayloadAction<MobileNodeSchema>) => {
      const focusId = uuid();
      state.focusId = focusId;
      state.children.push({
        ...action.payload,
        id: focusId
      });
    },

    /** 在指定位置插入组件 */
    appendCom: (state, action) => {
      const { data, item, hoverParentId, hoverIndex, positionDown } = action.payload;
      const focusId = uuid();

      traverse(state, sub => {
        // 非容器节点往父层插入
        if (!isContainerNode(data.type) && sub.id === hoverParentId) {
          if (positionDown) {
            sub.children.splice(hoverIndex + 1, 0, { ...item, id: focusId });
          } else {
            sub.children.splice(hoverIndex, 0, { ...item, id: focusId });
          }
          return false;
        }
        // 容器节点直接追加子节点
        if (isContainerNode(data.type) && sub.id === data.id) {
          if (sub.children && sub.children.length > 0) {
            sub.children.push({ ...item, id: focusId });
          } else {
            sub.children = [{ ...item, id: focusId }];
          }
          return false;
        }
        return true;
      });
      state.focusId = focusId;
    },

    /** 移动组件 */
    moveCom: (state, action) => {
      const {
        data: hoverData,
        item: dragData,
        hoverParentId: hId,
        hoverIndex: hIndex,
        dragParentId,
        dragIndex,
        positionDown
      } = action.payload;

      if (hoverData.id === dragData.id) return state;

      // 检查是否拖入自身子节点
      let hoverInDragData = false;
      traverse(dragData, sub => {
        if (sub.id === hoverData.id) {
          hoverInDragData = true;
          return false;
        }
        return true;
      });
      if (hoverInDragData) return state;

      const focusId = uuid();

      // 从原位置移除
      traverse(state, sub => {
        if (sub.id === dragParentId) {
          sub.children.splice(dragIndex, 1);
          return false;
        }
        return true;
      });

      // 插入到新位置
      traverse(state, sub => {
        if (!isContainerNode(hoverData.type) && sub.id === hId) {
          if (positionDown) {
            sub.children.splice(hIndex + 1, 0, { ...dragData, id: focusId });
          } else {
            sub.children.splice(hIndex, 0, { ...dragData, id: focusId });
          }
          return false;
        }
        if (isContainerNode(hoverData.type) && sub.id === hoverData.id) {
          if (sub.children) {
            sub.children.unshift({ ...dragData, id: focusId });
          } else {
            sub.children = [{ ...dragData, id: focusId }];
          }
          return false;
        }
        return true;
      });

      state.focusId = focusId;
    },

    /** 设置焦点 */
    setFocus: (state, action: PayloadAction<{ focusId: string }>) => {
      state.focusId = action.payload.focusId;
    },

    /** 删除组件 */
    removeCom: (state, action: PayloadAction<{ id: string; parentId: string }>) => {
      const { id, parentId } = action.payload;
      traverse(state, sub => {
        if (sub.id === parentId) {
          sub.children = sub.children.filter(child => child.id !== id);
          return false;
        }
        return true;
      });
    },

    /** 更新属性 */
    updateTree: (state, action: PayloadAction<{ key: string; value: any }>) => {
      const { focusId } = state;
      const { key, value } = action.payload;
      traverse(state, sub => {
        if (sub.id === focusId) {
          sub.props[key] = value;
          return false;
        }
        return true;
      });
    },

    /** 进入 Item 编辑模式 */
    setEditingItem: (state, action: PayloadAction<{ listNodeId: string }>) => {
      state.editingItem = true;
      state.listNodeId = action.payload.listNodeId;
    },

    /** 退出 Item 编辑模式 */
    exitEditingItem: state => {
      state.editingItem = false;
      state.listNodeId = undefined;
    },

    /** 从 JSON 加载配置 */
    loadConfig: (state, action: PayloadAction<MobileNodeSchema>) => {
      const config = action.payload;
      state.id = config.id || "root";
      state.type = config.type || "page";
      state.props = config.props || {};
      state.children = config.children || [];
      state.focusId = undefined;
      state.editingItem = false;
      state.listNodeId = undefined;
    },

    /** 重置编辑器 */
    resetEditor: () => {
      return { ...initialState, children: [] };
    }
  }
});

/** 判断是否为容器节点 */
function isContainerNode(type: string): boolean {
  return ["list", "row", "column"].includes(type);
}

export const {
  append,
  appendCom,
  moveCom,
  setFocus,
  removeCom,
  updateTree,
  setEditingItem,
  exitEditingItem,
  loadConfig,
  resetEditor
} = mobileEditorSlice.actions;

export default mobileEditorSlice.reducer;
