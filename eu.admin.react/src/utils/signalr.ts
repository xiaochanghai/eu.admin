import { HubConnectionBuilder, LogLevel } from "@microsoft/signalr";

let baseURL = import.meta.env.VITE_API_URL as string;
let VITE_USER_NODE_ENV = import.meta.env.VITE_USER_NODE_ENV as string;
const signalr = new HubConnectionBuilder()
  .withUrl((VITE_USER_NODE_ENV == "development" ? baseURL : "") + "/chat")
  .configureLogging(LogLevel.Information)
  .build();

export default signalr;
