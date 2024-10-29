// ? Not used yet, currently use global Loading to control repeated requests
import { CustomAxiosRequestConfig } from "../index";
import qs from "qs";

// Declare a Map to store the identity and cancel function for each request
let pendingMap = new Map<string, AbortController>();

// Serialize parameters to ensure consistent order of object properties
const sortedStringify = (obj: any) => {
  return qs.stringify(obj, { arrayFormat: "repeat", sort: (a, b) => a.localeCompare(b) });
};
// Serialization parameters
export const getPendingUrl = (config: CustomAxiosRequestConfig) => {
  return [config.method, config.url, sortedStringify(config.data), sortedStringify(config.params)].join("&");
};
export class AxiosCanceler {
  /**
   * @description: Add request
   * @param {Object} config
   * @return void
   */
  addPending(config: CustomAxiosRequestConfig) {
    // Before the request starts, check the previous request to cancel the operation
    this.removePending(config);
    const url = getPendingUrl(config);
    const controller = new AbortController();
    config.signal = controller.signal;
    pendingMap.set(url, controller);
  }

  /**
   * @description: Removal request
   * @param {Object} config
   */
  removePending(config: CustomAxiosRequestConfig) {
    const url = getPendingUrl(config);
    // If there is a current request ID in pending, the current request needs to be canceled
    const controller = pendingMap.get(url);
    if (controller) {
      controller.abort();
      pendingMap.delete(url);
    }
  }

  /**
   * @description: Clear all pending
   */
  removeAllPending() {
    pendingMap.forEach(controller => {
      controller && controller.abort();
    });
    pendingMap.clear();
  }
}
