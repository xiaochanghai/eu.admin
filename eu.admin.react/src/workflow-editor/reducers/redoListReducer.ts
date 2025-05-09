import { Action, ActionType, UnRedoListAction } from "../actions";
import { ISnapshot } from "../interfaces/state";

export function redoListReducer(state: ISnapshot[], action: Action): ISnapshot[] {
  switch (action.type) {
    case ActionType.SET_REDOLIST: {
      return (action as UnRedoListAction).payload.list;
    }
  }
  return state;
}
