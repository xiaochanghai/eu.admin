import axios, { AxiosInstance, AxiosError, AxiosRequestConfig, InternalAxiosRequestConfig, AxiosResponse } from "axios";
import { showFullScreenLoading, tryHideFullScreenLoading } from "@/components/Loading/fullScreen";
import { LOGIN_URL } from "@/config";
import { ResultData, ResultPage } from "@/api/interface";
import { ResultEnum } from "@/enums/httpEnum";
import { message } from "@/hooks/useMessage";
import { setToken } from "@/redux/modules/user";
import { checkStatus } from "./helper/checkStatus";
import { store } from "@/redux";

export interface CustomAxiosRequestConfig extends InternalAxiosRequestConfig {
  loading?: boolean;
}

const config = {
  // The default address request address, which can be modified in the .env.** file
  baseURL: import.meta.env.VITE_API_URL as string,
  // timeout
  timeout: ResultEnum.TIMEOUT as number,
  // Credentials are allowed to be carried across domains
  withCredentials: false
};

class RequestHttp {
  service: AxiosInstance;
  public constructor(config: AxiosRequestConfig) {
    // instantiation
    this.service = axios.create(config);

    /**
     * @description request interceptor
     * Client sends request -> [request interceptor] -> server
     * token verification (JWT): Accept the token returned by the server and store it in redux/local storage
     */
    this.service.interceptors.request.use(
      (config: CustomAxiosRequestConfig) => {
        // The current request needs to display loading, which is controlled by the third parameter specified in the API service: {loading: true}
        config.loading && showFullScreenLoading();
        if (config.headers && typeof config.headers.set === "function") {
          config.headers.set("Authorization", "Bearer " + store.getState().user.token);
        }
        return config;
      },
      (error: AxiosError) => {
        return Promise.reject(error);
      }
    );

    /**
     * @description response interceptor
     *  The server returns the information -> [intercept unified processing] -> the client JS gets the information
     */
    this.service.interceptors.response.use(
      (response: AxiosResponse) => {
        const { data } = response;
        tryHideFullScreenLoading();
        // login failure
        message.destroy();
        if (data.Status == ResultEnum.OVERDUE) {
          store.dispatch(setToken(""));
          message.error(data.Message);
          window.$navigate(LOGIN_URL);
          return Promise.reject(data);
        }
        // Global error information interception (to prevent data stream from being returned when downloading files, and report errors directly without code)
        if (data.Status && data.Status !== ResultEnum.SUCCESS) {
          message.error(data.Message);
          return Promise.reject(data);
        }
        // Successful request (no need to handle failure logic on the page unless there are special circumstances)
        return data;
      },
      async (error: AxiosError) => {
        const { response } = error;
        message.destroy();
        tryHideFullScreenLoading();
        // Request timeout && network error judged separately, no response
        if (error.message.indexOf("timeout") !== -1) message.error("请求超时！请您稍后重试");
        if (error.message.indexOf("Network Error") !== -1) message.error("网络错误！请您稍后重试");
        // Do different processing according to the error status code of the server response
        if (response) checkStatus(response.status);
        // The server does not return any results (maybe the server is wrong or the client is disconnected from the network), disconnection processing: you can jump to the disconnection page
        if (!window.navigator.onLine) window.$navigate("/500");
        return Promise.reject(error);
      }
    );
  }

  /**
   * @description Common request method encapsulation
   */
  get<T>(url: string, params?: object, _object = {}): Promise<ResultData<T>> {
    return this.service.get(url, { params, ..._object });
  }
  post<T>(url: string, params?: object | string, _object = {}): Promise<ResultData<T>> {
    return this.service.post(url, params, _object);
  }
  put<T>(url: string, params?: object, _object = {}): Promise<ResultData<T>> {
    return this.service.put(url, params, _object);
  }
  delete<T>(url: string, params?: object): Promise<ResultData<T>> {
    return this.service.delete(url, {
      data: params
    });
  }
  download(url: string, params?: object, _object = {}): Promise<BlobPart> {
    return this.service.post(url, params, { ..._object, responseType: "blob" });
  }
  getGridList(url: string, params?: object, _object = {}): Promise<ResultPage> {
    return this.service.get(url, { params, ..._object });
  }
  postForm<T>(url: string, params?: object): Promise<ResultData<T>> {
    return this.service.postForm(url, params);
  }
}

export default new RequestHttp(config);
