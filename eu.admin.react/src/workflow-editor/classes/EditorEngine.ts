// import { Store } from "redux";
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
import { IWorkFlowNode } from "../interfaces";
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
// import { createUuid } from "../utils/create-uuid";
import { SET_UNOLIST, SET_REDOLIST, SET_START_NODE } from "@/redux/modules/workflow";

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

  // validate = () => {
  //   return true;
  // };

  backup = () => {
    const list = [...this.state.undoList, { startNode: this.state.startNode, validated: this.state.validated }];

    this.dispatch(SET_UNOLIST(list));
    this.dispatch(SET_REDOLIST([]));
  };

  setStartNode = (node: IWorkFlowNode) => {
    this.backup();
    this.dispatch(SET_START_NODE(node));
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
}
