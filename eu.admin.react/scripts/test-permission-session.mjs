import assert from "node:assert/strict";
import fs from "node:fs";
import vm from "node:vm";
import { test } from "node:test";
import ts from "typescript";
import * as toolkit from "@reduxjs/toolkit";

// Execute the real reducers/hook with isolated API promises; no backend access.
function load(path, modules) {
  const source = fs.readFileSync(new URL(path, import.meta.url), "utf8");
  const code = ts.transpileModule(source, { compilerOptions: { module: ts.ModuleKind.CommonJS } }).outputText;
  const context = { exports: {}, require: id => modules[id], DOMException };
  vm.runInNewContext(code, context);
  return context.exports;
}

function setup() {
  const user = load("../src/redux/modules/user.ts", { "@reduxjs/toolkit": toolkit });
  const auth = load("../src/redux/modules/auth.ts", {
    "@reduxjs/toolkit": toolkit,
    "./user": user,
    "@/utils": { getFlatMenuList: value => value, getShowMenuList: value => value }
  });
  const store = toolkit.configureStore({ reducer: { user: user.default, auth: auth.default } });
  const requests = [];
  const permissions = load("../src/hooks/usePermissions.ts", {
    react: { useCallback: fn => fn },
    "@/redux": { store, useDispatch: () => store.dispatch },
    "@/redux/modules/user": user,
    "@/redux/modules/auth": auth,
    "@/hooks/useMessage": { notification: { warning() {} } },
    "@/api/modules/login": {
      getAuthButtonListApi: () => ({ Data: { page: ["read"] } }),
      getAuthMenuListApi: () => new Promise((resolve, reject) => requests.push({ resolve, reject }))
    }
  });
  return { store, user, auth, requests, init: permissions.default().initPermissions };
}

test("expiry and new login clear all old permissions before navigation can run", () => {
  const { store, user, auth } = setup();
  store.dispatch(user.setToken("A"));
  store.dispatch(auth.setAuthMenuList([{ path: "/old-page" }]));
  store.dispatch(auth.setAuthButtonList({ page: ["write"] }));
  store.dispatch(user.setToken(""));
  store.dispatch(user.setToken("B"));
  const state = store.getState();
  for (const key of ["authMenuList", "flatMenuList", "showMenuList"]) assert.equal(state.auth[key].length, 0);
  assert.equal(Object.keys(state.auth.authButtonList).length, 0);
  assert.equal(Boolean(state.user.token && state.auth.authMenuList.length), false);
});

test("deduplicates within a session but rejects a previous session with the same token", async () => {
  const { store, user, requests, init } = setup();
  store.dispatch(user.setToken("A"));
  const oldRequest = init("A");
  assert.equal(init("A"), oldRequest);
  await Promise.resolve();
  store.dispatch(user.setToken(""));
  store.dispatch(user.setToken("A"));
  const newRequest = init("A");
  assert.notEqual(newRequest, oldRequest);
  await Promise.resolve();
  requests[0].resolve({ Data: [{ path: "/old-page" }] });
  await assert.rejects(oldRequest, { name: "AbortError" });
  assert.equal(store.getState().auth.authMenuList.length, 0);
  assert.equal(init("A"), newRequest);
  requests[1].resolve({ Data: [{ path: "/new-page" }] });
  await newRequest;
  assert.equal(store.getState().auth.authMenuList[0].path, "/new-page");
});

test("an old request failure cannot clear the current session", async () => {
  const { store, user, requests, init } = setup();
  store.dispatch(user.setToken("A"));
  const request = init("A");
  await Promise.resolve();
  store.dispatch(user.setToken("B"));
  requests[0].reject(new Error("old network failure"));
  await assert.rejects(request);
  assert.equal(store.getState().user.token, "B");
});
