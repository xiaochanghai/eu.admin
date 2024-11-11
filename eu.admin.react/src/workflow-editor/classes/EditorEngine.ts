// import { Store } from "redux";
import { IErrors } from "../interfaces/state";
// import { configureStore } from "@reduxjs/toolkit";
// import { mainReducer } from "../reducers";
import { store as store1 } from "@/redux";
import {
  ErrorsListener,
  RedoListChangeListener,
  SelectedListener,
  StartNodeListener,
  UndoListChangeListener
} from "../interfaces/listeners";
import { IBranchNode, IRouteNode, IWorkFlowNode, NodeType } from "../interfaces";
// import {
//   Action,
//   ActionType,
//   AddNodeAction,
//   ChangeNodeAction,
//   DeleteNodeAction,
//   SelectNodeAction,
//   SetErrorsAction,
//   SetStartNodeAction,
//   SetValidatedAction,
//   UnRedoListAction
// } from "../actions";
import { IMaterialUIs, INodeMaterial, Translate } from "../interfaces/material";
import { createUuid } from "../utils/create-uuid";
import {
  SET_ERRORS,
  SET_VALIDATED,
  SET_UNOLIST,
  SET_REDOLIST,
  SET_START_NODE,
  CHANGE_NODE,
  ADD_NODE,
  SELECT_NODE,
  DELETE_NODE
} from "@/redux/modules/workflow";

import { dispatch } from "@/utils";

//dlc 流程编辑引擎
export class EditorEngine {
  store: any; // dlc 流程IState状态
  state: any;
  dispatch: any; // dlc 流程IState状态
  t: Translate = (msg: string) => msg; //国际化的取值
  materials: INodeMaterial[] = []; //dlc 节点物料
  materialUis: IMaterialUIs = {}; //dlc 物料ui的map
  constructor() {
    this.store = store1;
    this.dispatch = dispatch;
    this.state = this.store.getState().workflow;
  }

  getNode(nodeId: string, parentNode?: IWorkFlowNode): IWorkFlowNode | undefined {
    const startNode = parentNode || this.store.getState().workflow.startNode;
    if (startNode?.id === nodeId && nodeId) {
      return startNode;
    }
    if (startNode?.childNode) {
      const foundNode = this.getNode(nodeId, startNode?.childNode);
      if (foundNode) {
        return foundNode;
      }
    }
    if (startNode?.nodeType === NodeType.route) {
      for (const conditionNode of (startNode as IRouteNode).conditionNodeList) {
        const foundNode = this.getNode(nodeId, conditionNode);
        if (foundNode) {
          return foundNode;
        }
      }
    }
    return undefined;
  }

  validate = () => {
    this.dispatch(SET_VALIDATED(true));

    const errors: IErrors = {};
    this.setErrors({});
    this.doValidateNode(this.store.getState().workflow.startNode, errors);
    if (Object.keys(errors).length > 0) {
      this.setErrors(errors);
      return errors;
    }
    return true;
  };

  setErrors = (errors: IErrors) => {
    this.dispatch(SET_ERRORS(errors));
  };
  backup = () => {
    const list = [...this.state.undoList, { startNode: this.state.startNode, validated: this.state.validated }];

    this.dispatch(SET_UNOLIST(list));
    this.dispatch(SET_REDOLIST([]));
  };

  //dlc 取消
  undo = () => {
    const newUndoList = [...this.state.undoList];
    const snapshot = newUndoList.pop();
    if (!snapshot) {
      console.error("No element in undo list");
      return;
    }

    this.dispatch(SET_UNOLIST(newUndoList));

    const list = [...this.state.redoList, { startNode: this.state.startNode }];

    this.dispatch(SET_REDOLIST(list));
    this.dispatch(SET_START_NODE(snapshot?.startNode));
    this.dispatch(SET_VALIDATED(snapshot?.validated));
  };

  // dlc 重做
  redo = () => {
    const newRedoList = [...this.state.redoList];
    const snapshot = newRedoList.pop();
    if (!snapshot) {
      console.error("No element in redo list");
      return;
    }

    this.dispatch(SET_REDOLIST(newRedoList));

    this.dispatch(SET_UNOLIST([...this.state.undoList, { startNode: this.state.startNode }]));

    this.dispatch(SET_START_NODE(snapshot?.startNode));
    this.dispatch(SET_VALIDATED(snapshot?.validated));
  };

  setStartNode = (node: IWorkFlowNode) => {
    this.backup();
    this.dispatch(SET_START_NODE(node));

    this.revalidate();
  };

  changeNode = (node: IWorkFlowNode) => {
    this.backup();

    this.dispatch(CHANGE_NODE(node));

    this.revalidate();
  };

  addCondition(node: IRouteNode, condition: IBranchNode) {
    const newNode: IRouteNode = { ...node, conditionNodeList: [...node.conditionNodeList, condition] };
    this.changeNode(newNode);
  }

  changeCondition(node: IRouteNode, condition: IBranchNode) {
    const newNode: IRouteNode = {
      ...node,
      conditionNodeList: node.conditionNodeList.map(con => (con.id === condition.id ? condition : con))
    };
    this.changeNode(newNode);
  }

  removeCondition(node: IRouteNode, conditionId: string) {
    //如果只剩2个分支，则删除节点
    if (node.conditionNodeList.length <= 2) {
      this.removeNode(node.id);
      return;
    }
    this.backup();
    const newNode: IRouteNode = { ...node, conditionNodeList: node.conditionNodeList.filter(co => co.id !== conditionId) };
    this.changeNode(newNode);
  }

  //条件左移一位
  transConditionOneStepToLeft(node: IRouteNode, index: number) {
    if (index > 0) {
      this.backup();
      const newConditions = [...node.conditionNodeList];
      newConditions[index] = newConditions.splice(index - 1, 1, newConditions[index])[0];
      const newNode: IRouteNode = { ...node, conditionNodeList: newConditions };
      this.changeNode(newNode);
    }
  }

  //条件右移一位
  transConditionOneStepToRight(node: IRouteNode, index: number) {
    const newConditions = [...node.conditionNodeList];
    if (index < newConditions.length - 1) {
      this.backup();
      newConditions[index] = newConditions.splice(index + 1, 1, newConditions[index])[0];
      const newNode: IRouteNode = { ...node, conditionNodeList: newConditions };
      this.changeNode(newNode);
    }
  }

  //克隆一个条件
  cloneCondition(node: IRouteNode, condition: IBranchNode) {
    const newCondition = JSON.parse(JSON.stringify(condition));
    newCondition.name = newCondition.name + this.t?.("ofCopy");
    //重写Id
    resetId(newCondition);
    const index = node.conditionNodeList.indexOf(condition);
    const newList = [...node.conditionNodeList];
    newList.splice(index + 1, 0, newCondition);
    const newNode: IRouteNode = { ...node, conditionNodeList: newList };
    this.changeNode(newNode);
  }

  addNode = (parentId: string, node: IWorkFlowNode) => {
    this.backup();
    this.dispatch(ADD_NODE({ parentId, node }));
    this.revalidate();
  };

  selectNode = (id: string | undefined) => {
    // this.store.getState().workflow.selectedId = id;
    this.dispatch(SELECT_NODE(id));
  };

  removeNode = (id?: string) => {
    if (id) {
      this.backup();
      this.dispatch(DELETE_NODE(id));
      this.revalidate();
    }
  };

  subscribeStartNodeChange(listener: StartNodeListener) {
    let previousState: IWorkFlowNode | undefined = this.store.getState().workflow.startNode;

    const handleChange = () => {
      const nextState = this.store.getState().workflow.startNode;
      if (nextState === previousState) {
        return;
      }
      previousState = nextState;
      listener(nextState);
    };
    handleChange();
    return this.store.subscribe(handleChange);
  }

  subscribeSelectedChange = (listener: SelectedListener) => {
    let previousState: string | undefined = this.state.selectedId;

    const handleChange = () => {
      const nextState = this.state;
      if (nextState === previousState) {
        return;
      }
      previousState = nextState;
      listener(nextState);
    };
    return this.store.subscribe(handleChange);
  };

  subscribeUndoListChange(listener: UndoListChangeListener) {
    let previousState = this.store.getState().workflow.undoList;

    const handleChange = () => {
      const nextState = this.store.getState().workflow.undoList;
      if (nextState === previousState) {
        return;
      }
      previousState = nextState;
      listener(nextState);
    };
    return this.store.subscribe(handleChange);
  }

  subscribeRedoListChange(listener: RedoListChangeListener) {
    let previousState = this.store.getState().workflow.redoList;

    const handleChange = () => {
      const nextState = this.store.getState().workflow.redoList;
      if (nextState === previousState) {
        return;
      }
      previousState = nextState;
      listener(nextState);
    };
    return this.store.subscribe(handleChange);
  }

  subscribeErrorsChange(listener: ErrorsListener) {
    let previousState = this.store.getState().workflow.errors;

    const handleChange = () => {
      const nextState = this.store.getState().workflow.errors;
      if (nextState === previousState) {
        return;
      }
      previousState = nextState;
      listener(nextState);
    };
    return this.store.subscribe(handleChange);
  }

  //审批流节点不多，节点变化全部重新校验一遍，无需担心性能问题，以后有需求再优化
  private revalidate = () => {
    if (this.store.getState().workflow.validated) {
      this.validate();
    }
  };

  private doValidateNode = (node: IWorkFlowNode, errors: IErrors) => {
    const materialUi = this.materialUis[node.nodeType];
    if (materialUi?.validate) {
      const result = materialUi.validate(node, { t: this.t });
      if (result !== true && result !== undefined) {
        errors[node.id] = result;
      }
    }
    if (node.childNode) {
      this.doValidateNode(node.childNode, errors);
    }
    if (node.nodeType === NodeType.route) {
      for (const condition of (node as IRouteNode).conditionNodeList) {
        this.doValidateNode(condition, errors);
      }
    }
  };
}

function resetId(node: IWorkFlowNode) {
  node.id = createUuid();
  if (node.childNode) {
    resetId(node.childNode);
  }
  if (node.nodeType === NodeType.route) {
    for (const condition of (node as IRouteNode).conditionNodeList) {
      resetId(condition);
    }
  }
}

// function makeStoreInstance(debugMode: boolean): Store<IState> {
//   // TODO: if we ever make a react-native version of this,
//   // we'll need to consider how to pull off dev-tooling
//   const reduxDevTools =
//     typeof window !== "undefined" &&
//     // eslint-disable-next-line @typescript-eslint/no-explicit-any
//     (window as any).__REDUX_DEVTOOLS_EXTENSION__;
//   return configureStore({
//     reducer: mainReducer,
//     middleware: getDefaultMiddleware =>
//       getDefaultMiddleware({
//         immutableCheck: false,
//         serializableCheck: false
//       }),
//     devTools:
//       debugMode &&
//       reduxDevTools &&
//       reduxDevTools({
//         name: "dnd-core",
//         instanceId: "dnd-core"
//       })
//   });
// }
