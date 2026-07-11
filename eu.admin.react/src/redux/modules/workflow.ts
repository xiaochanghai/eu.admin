import { createSlice, PayloadAction } from "@reduxjs/toolkit";
// import { ModuleInfo } from "@/api/interface/index";
// import { IWorkFlowNode } from "@/workflow-editor/interfaces";
import { NodeType } from "@/workflow-editor/interfaces";
import { modifyWorkFlowStartNode, modifyNodeName } from "@/utils";
import { ActionType } from "@/workflow-editor/actions";

interface IErrors {
  [nodeId: string]: string | undefined;
}
const state: any = {
  errors: {},
  validated: null,
  undoList: [],
  redoList: [],
  changeNode: {},
  selectedId: null,
  changeFlag: false,
  formId: null as string | null, // 当前设计器的 formVo.id，供 PublishButton 使用
  startNode: {
    id: "start",
    nodeType: NodeType.start
  }
};

const workflowSlice = createSlice({
  name: "hooks-workflow",
  initialState: state,
  reducers: {
    SET_ERRORS(state, { payload }: PayloadAction<IErrors>) {
      state.errors = payload;
    },
    SET_VALIDATED(state, { payload }: PayloadAction<boolean>) {
      state.validated = payload;
    },
    SET_UNOLIST(state, { payload }: PayloadAction<any>) {
      state.undoList = payload;
    },
    SET_REDOLIST(state, { payload }: PayloadAction<any>) {
      state.redoList = payload;
    },
    SET_FORM_ID(state, { payload }: PayloadAction<string | null>) {
      state.formId = payload;
    },
    SET_START_NODE(state, { payload }: PayloadAction<any>) {
      state.startNode = payload;
    },
    CHANGE_NODE(state, { payload }: PayloadAction<any>) {
      // 递归更新节点的辅助函数
      const updateNode = (node: any): any => {
        // 找到目标节点，合并 payload
        if (node.id === payload.id) {
          return { ...node, ...payload };
        }

        // 创建新对象副本
        const updates: any = { ...node };

        // 递归处理 childNode
        if (node.childNode) {
          updates.childNode = updateNode(node.childNode);
        }

        // 递归处理 conditionNodeList
        if (node.conditionNodeList) {
          updates.conditionNodeList = node.conditionNodeList.map((condNode: any) => {
            if (condNode.id === payload.id) {
              return { ...condNode, ...payload };
            }
            if (condNode.childNode) {
              return { ...condNode, childNode: updateNode(condNode.childNode) };
            }
            return condNode;
          });
        }

        return updates;
      };

      // 更新 startNode
      state.startNode = updateNode(state.startNode);
    },
    ADD_NODE(state, { payload }: PayloadAction<any>) {
      // let code = JSON.stringify(state.startNode);
      // debugger;
      if (state.startNode.id === payload.parentId)
        state.startNode = { ...state.startNode, childNode: { ...payload.node, childNode: state.startNode.childNode } };
      else modifyWorkFlowStartNode(ActionType.ADD_NODE, state.startNode, payload.node, payload.parentId);
    },
    SELECT_NODE(state, { payload }: PayloadAction<any>) {
      state.selectedId = payload;
    },
    DELETE_NODE(state, { payload }: PayloadAction<any>) {
      state.selectedId = payload;
      if (payload === state.startNode.childNode?.id)
        state.startNode = { ...state.startNode, childNode: state.startNode.childNode.childNode };
      else modifyWorkFlowStartNode(ActionType.DELETE_NODE, state.startNode, payload.node, payload.parentId);
    },
    ADD_CONDITION(state, { payload }: PayloadAction<any>) {
      if (state.startNode.id === payload.id) state.startNode.conditionNodeList = payload.conditionNodeList;
      else modifyWorkFlowStartNode(ActionType.ADD_CONDITION, state.startNode, payload, payload.id);
    },
    REMOVE_CONDITION(state, { payload }: PayloadAction<any>) {
      if (state.startNode.id === payload.id) state.startNode.conditionNodeList = payload.conditionNodeList;
      else modifyWorkFlowStartNode(ActionType.ADD_CONDITION, state.startNode, payload, payload.id);
    },
    MODIFY_CONDITION(state, { payload }: PayloadAction<any>) {
      if (state.startNode.id === payload.id) state.startNode.conditionNodeList = payload.conditionNodeList;
      else modifyWorkFlowStartNode(ActionType.ADD_CONDITION, state.startNode, payload, payload.id);
    },
    MODIFY_NODE_NAME(state, { payload }: PayloadAction<any>) {
      if (state.startNode.id === payload.id) state.startNode.conditionNodeList = payload.conditionNodeList;
      else modifyNodeName(state.startNode, payload.name, payload.node);
    }
  }
});

export const {
  SET_ERRORS,
  SET_UNOLIST,
  SET_VALIDATED,
  SET_REDOLIST,
  SET_START_NODE,
  SET_FORM_ID,
  DELETE_NODE,
  CHANGE_NODE,
  SELECT_NODE,
  ADD_NODE,
  ADD_CONDITION,
  REMOVE_CONDITION,
  MODIFY_CONDITION,
  MODIFY_NODE_NAME
} = workflowSlice.actions;
export default workflowSlice.reducer;
