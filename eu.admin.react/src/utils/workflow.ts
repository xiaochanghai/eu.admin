import { useEffect } from "react";
import { useDispatch } from "@/redux";
import { store } from "@/redux";
import {
  SET_ERRORS,
  SET_VALIDATED,
  SET_UNOLIST,
  SET_REDOLIST,
  // SET_START_NODE,
  CHANGE_NODE,
  ADD_NODE,
  SELECT_NODE
  // DELETE_NODE
} from "@/redux/modules/workflow";
import { useEditorEngine } from "../workflow-editor/hooks";
import { IErrors } from "../workflow-editor/interfaces/state";
import { IRouteNode, IWorkFlowNode, NodeType } from "../workflow-editor/interfaces";

export function useWorkFlow() {
  const dispatch = useDispatch();
  const editorStore = useEditorEngine();
  useEffect(() => {}, []);
  function addNode(parentId: string, node: IWorkFlowNode) {
    backup();
    dispatch(ADD_NODE({ parentId, node }));

    revalidate();
  }
  function backup() {
    const list = [
      ...store.getState().workflow.undoList,
      { startNode: store.getState().workflow.startNode, validated: store.getState().workflow.validated }
    ];

    dispatch(SET_UNOLIST(list));
    dispatch(SET_REDOLIST([]));
  }
  //审批流节点不多，节点变化全部重新校验一遍，无需担心性能问题，以后有需求再优化
  function revalidate() {
    if (store.getState().workflow.validated) {
      validate();
    }
  }

  function validate() {
    dispatch(SET_VALIDATED(true));

    const errors: IErrors = {};
    setErrors({});
    doValidateNode(store.getState().workflow.startNode, errors);
    if (Object.keys(errors).length > 0) {
      setErrors(errors);
      return errors;
    }
    return true;
  }
  function setErrors(errors: IErrors) {
    dispatch(SET_ERRORS(errors));
  }

  function doValidateNode(node: IWorkFlowNode, errors: IErrors) {
    const materialUi = editorStore?.materialUis[node.nodeType];
    if (materialUi?.validate) {
      if (editorStore != null) {
        let t = { t: editorStore.t };
        const result = materialUi.validate(node, t);
        if (result !== true && result !== undefined) errors[node.id] = result;
      }
    }
    if (node.childNode) doValidateNode(node.childNode, errors);
    if (node.nodeType === NodeType.route) {
      for (const condition of (node as IRouteNode).conditionNodeList) {
        doValidateNode(condition, errors);
      }
    }
  }
  function selectNode(id: string | undefined) {
    dispatch(SELECT_NODE(id));
  }
  function changeNode(node: any) {
    backup();

    dispatch(CHANGE_NODE(node));

    revalidate();
  }
  return { validate, addNode, selectNode, changeNode };
}
