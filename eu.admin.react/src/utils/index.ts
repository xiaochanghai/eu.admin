import { store, useDispatch } from "@/redux";
import { ResPage } from "@/api/interface";
import { RouteObjectType } from "@/routers/interface";
import { RequestData } from "@ant-design/pro-components";
import { v4 as uuid4 } from "uuid";
import { ActionType } from "@/workflow-editor/actions";

const mode = import.meta.env.VITE_ROUTER_MODE;

/**
 * @description Get the corresponding greeting for the current time.
 * @returns {String}
 */
export function getTimeState() {
  let timeNow = new Date();
  let hours = timeNow.getHours();
  if (hours >= 6 && hours <= 10) return `早上好 ⛅`;
  if (hours >= 10 && hours <= 14) return `中午好 🌞`;
  if (hours >= 14 && hours <= 18) return `下午好 🌞`;
  if (hours >= 18 && hours <= 24) return `晚上好 🌛`;
  if (hours >= 0 && hours <= 6) return `凌晨好 🌛`;
}

/**
 * @description Generate random numbers
 * @param {Number} min minimum value
 * @param {Number} max Maximum value
 * @return {Number}
 */
export function randomNum(min: number, max: number): number {
  let num = Math.floor(Math.random() * (min - max) + max);
  return num;
}

/**
 * @description Set style properties
 * @param {String} key - The key name of the style property
 * @param {String} val - The value of the style attribute
 */
export function setStyleProperty(key: string, val: string) {
  document.documentElement.style.setProperty(key, val);
}

/**
 * @description Convert a 3-digit HEX color code to a 6-digit code.
 * @returns {String}
 */
export function convertToSixDigitHexColor(str: string) {
  if (str.length > 4) return str.toLocaleUpperCase();
  else return (str[0] + str[1] + str[1] + str[2] + str[2] + str[3] + str[3]).toLocaleUpperCase();
}

/**
 * @description Get the default language of the browser.
 * @returns {String}
 */
export function getBrowserLang() {
  let browserLang = navigator.language ? navigator.language : navigator.browserLanguage;
  let defaultBrowserLang = "";
  if (["cn", "zh", "zh-cn"].includes(browserLang.toLowerCase())) defaultBrowserLang = "zh";
  else defaultBrowserLang = "en";
  return defaultBrowserLang;
}

/**
 * @description Flatten the menu using recursion for easier addition of dynamic routes.
 * @param {Array} menuList - The menu list.
 * @returns {Array}
 */
export function getFlatMenuList(menuList: RouteObjectType[]): RouteObjectType[] {
  let newMenuList: RouteObjectType[] = JSON.parse(JSON.stringify(menuList));
  return newMenuList.flatMap(item => [item, ...(item.children ? getFlatMenuList(item.children) : [])]);
}

/**
 * @description Use recursion to filter out the menu items that need to be rendered in the left menu (excluding menus with isHide == true).
 * @param {Array} menuList - The menu list.
 * @returns {Array}
 */
export function getShowMenuList(menuList: RouteObjectType[]) {
  let newMenuList: RouteObjectType[] = JSON.parse(JSON.stringify(menuList));
  return newMenuList.filter(item => {
    item.children?.length && (item.children = getShowMenuList(item.children));
    return !item.meta?.isHide;
  });
}

/**
 * @description Obtain the first level menu
 * @param {RouteObjectType[]} menuList - The menu list.
 * @returns {RouteObjectType[]}
 */
export function getFirstLevelMenuList(menuList: RouteObjectType[]) {
  return menuList.map(item => {
    return { ...item, children: undefined };
  });
}

/**
 * @description Get a menu object with a path
 * @param {Array} menulist - The list of menu objects to search through.
 * @param {string} path - The path to match with the menu objects' paths.
 * @returns {Object} The matched menu object or null if no match is found.
 */
export function getMenuByPath(
  menulist: RouteObjectType[] = store.getState().auth.flatMenuList,
  path: string = getUrlWithParams()
) {
  const menuItem = menulist.find(menu => {
    // Match Dynamic routing through regular
    const regex = new RegExp(`^${menu.path?.replace(/:.[^/]*/, ".*")}$`);
    return regex.test(path);
  });
  return menuItem || {};
}

export function dispatch() {
  return useDispatch;
}

/**
 * @description Use recursion to find all breadcrumbs and store them in redux.
 * @param {Array} menuList - The menu list.
 * @param {Array} parent - The parent menu.
 * @param {Object} result - The processed result.
 * @returns {Object}
 */
export function getAllBreadcrumbList(
  menuList: RouteObjectType[],
  parent: RouteObjectType[] = [],
  result: { [key: string]: RouteObjectType[] } = {}
) {
  for (const item of menuList) {
    result[item.meta!.key!] = [...parent, item];
    if (item.children) getAllBreadcrumbList(item.children, result[item.meta!.key!], result);
  }
  return result;
}

/**
 * @description Get relative url with params
 * @returns {String}
 */
export function getUrlWithParams() {
  const url = {
    hash: location.hash.substring(1),
    history: location.pathname + location.search
  };
  return url[mode];
}

/**
 * @description Get the subMenu keys that need to be expanded.
 * @param {String} path - The current path.
 * @returns {Array}
 */
export function getOpenKeys(path: string): string[] {
  let currentKey: string = "";
  let openKeys: string[] = [];
  let pathSegments: string[] = path.split("/").map((segment: string) => "/" + segment);
  for (let i: number = 1; i < pathSegments.length - 1; i++) {
    currentKey += pathSegments[i];
    openKeys.push(currentKey);
  }
  return openKeys;
}

/**
 * @description Format the data returned by the server for the ProTable component.
 * @param {Object} data - The data returned by the server.
 * @returns {Object}
 */
export function formatDataForProTable<T>(data: ResPage<T>): Partial<RequestData<T>> {
  return {
    success: true,
    data: data.list,
    total: data.total
  };
}

/**
 * @description A function to execute a block of code and prevent debugging in the browser.
 * @returns {number} - The ID of the setInterval, which can be used to stop the execution later.
 */
export function blockDebugger() {
  function innerFunction() {
    try {
      // Prevent debugging by invoking the "debugger" statement using unconventional syntax
      (function () {
        return false;
      })
        ["constructor"]("debugger")
        ["call"]();
    } catch (err) {
      console.log("Debugger is blocked");
    }
  }
  // Start the execution using setInterval and return the interval ID
  return setInterval(innerFunction, 50);
}
/**
 * @description 下载文件
 * @param {string} fileId - 文件ID
 * @param {string} fileName - 文件名
 */
export function downloadFile(fileId: any, fileName: any) {
  let baseURL = import.meta.env.VITE_API_URL as string;
  const url = (baseURL ? baseURL : "") + `/api/File/Download/${fileId}`;

  const link = document.createElement("a");
  link.style.display = "none";
  link.href = url;
  link.setAttribute("download", fileName);

  document.body.appendChild(link);
  link.click();
  // 释放URL对象所占资源
  window.URL.revokeObjectURL(url);
  // 用完即删
  document.body.removeChild(link);
}
/**
 * @description Generate random string
 * @param {Number} length string length
 * @return {String}
 */
export function randomStr(length = 32) {
  const characters = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
  let result = "";
  for (let i = 0; i < length; i++) {
    result += characters.charAt(Math.floor(Math.random() * characters.length));
  }
  return result;
}
/**
 * @description Whether exists key  in keys
 * @param {String[]} keys string[]
 * @param {String} key string
 * @return {Boolean}
 */
export function some(keys: string[], key: string) {
  return keys.some((value: string) => value === key);
}
export function modifyWorkFlowStartNode(action: ActionType, obj: any, newValue: any, parentId: any): void {
  if (action == ActionType.ADD_NODE) {
    if (obj["nodeType"] == "route" || obj["nodeType"] == "condition") {
      if (obj["id"] == parentId) {
        obj["childNode"] = newValue;
      } else if (obj.conditionNodeList)
        obj.conditionNodeList.map((con: any) => {
          if (con["id"] == parentId) con["childNode"] = newValue;
          else if (con["childNode"]) modifyWorkFlowStartNode(action, con["childNode"], newValue, parentId);
        });
    } else if (obj["id"] == parentId) {
      obj["childNode"] = newValue;
    } else {
      if (obj["childNode"]) modifyWorkFlowStartNode(action, obj["childNode"], newValue, parentId);
    }
  } else if (action == ActionType.ADD_CONDITION) {
    if (obj["id"] == parentId) {
      obj["conditionNodeList"] = [...newValue.conditionNodeList];
    } else {
      if (obj["childNode"]) modifyWorkFlowStartNode(action, obj["childNode"], newValue, parentId);
    }
  }
}
export function modifyNodeName(obj: any, newValue: any, node: any): void {
  if (node.nodeType === "condition") {
    //条件节点
    if (obj["conditionNodeList"])
      obj["conditionNodeList"] = obj.conditionNodeList.map((con: any) => (con.id === node.id ? { ...con, name: newValue } : con));
    else if (obj["childNode"]) modifyNodeName(obj["childNode"], newValue, node);
    // if (obj["id"] == node.id) {
    //   if (obj["conditionNodeList"])
    //     obj["conditionNodeList"] = node.conditionNodeList.map((con: any) =>
    //       con.id === node.id ? { ...node, name: newValue } : con
    //     );
    //   // obj["name"] = newValue;
    // } else {
    //   if (obj["childNode"]) modifyNodeName(obj["childNode"], newValue, node);
    // }
  } else {
    if (obj["id"] == node.id) {
      obj["name"] = newValue;
    } else {
      if (obj["childNode"]) modifyNodeName(obj["childNode"], newValue, node);
    }
  }
}
/**
 * @description Generate uuid
 * @return {uuid}
 */
export function createUuid() {
  return uuid4();
}
export const traverse = <T extends { children?: T[] }>(data: T, fn: (param: T) => boolean) => {
  if (fn(data) === false) {
    return false;
  }

  if (data && data.children) {
    for (let i = data.children.length - 1; i >= 0; i--) {
      if (!traverse(data.children[i], fn)) return false;
    }
  }
  return true;
};

/**
 * depth first traverse, from leaves to root, children in inverse order
 *  if the fn returns false, terminate the traverse
 */
export const traverseUp = <T extends { children?: T[] }>(data: T, fn: (param: T) => boolean) => {
  if (data && data.children) {
    for (let i = data.children.length - 1; i >= 0; i--) {
      if (!traverseUp(data.children[i], fn)) return;
    }
  }

  if (fn(data) === false) {
    return false;
  }
  return true;
};
