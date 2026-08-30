import assert from "node:assert/strict";
import test from "node:test";
import {
  authorizedFetch,
  authorizedHeaders,
  clearAccessToken,
  getAccessToken,
  normalizeReturnUrl,
  setAccessToken
} from "../auth.js";

function createStorage() {
  const values = new Map();
  return {
    getItem: key => values.get(key) ?? null,
    setItem: (key, value) => values.set(key, String(value)),
    removeItem: key => values.delete(key)
  };
}

test("access token is kept in the current browser session", () => {
  global.window = {
    sessionStorage: createStorage(),
    location: { pathname: "/", search: "", hash: "", replace() {} }
  };
  try {
    setAccessToken(" token-value ");
    assert.equal(getAccessToken(), "token-value");
    clearAccessToken();
    assert.equal(getAccessToken(), "");
  } finally {
    delete global.window;
  }
});

test("authorized headers preserve existing values and add bearer token", () => {
  const headers = authorizedHeaders({ Accept: "application/json" }, "token-value");
  assert.equal(headers.get("Accept"), "application/json");
  assert.equal(headers.get("Authorization"), "Bearer token-value");
});

test("return URL stays on the Agent origin", () => {
  assert.equal(
    normalizeReturnUrl("/chat?source=login#latest", "https://agent.example.test"),
    "/chat?source=login#latest");
  assert.equal(
    normalizeReturnUrl("//evil.example.test/path", "https://agent.example.test"),
    "/");
  assert.equal(
    normalizeReturnUrl("/\\evil.example.test/path", "https://agent.example.test"),
    "/");
});

test("a 401 response clears the session and redirects to login", async () => {
  let redirectedTo = "";
  global.window = {
    sessionStorage: createStorage(),
    location: {
      pathname: "/",
      search: "",
      hash: "",
      replace: value => { redirectedTo = value; }
    }
  };
  global.fetch = async (_input, init) => {
    assert.equal(new Headers(init.headers).get("Authorization"), "Bearer token-value");
    return new Response(null, { status: 401 });
  };
  try {
    setAccessToken("token-value");
    await authorizedFetch("/api/agents");
    assert.equal(getAccessToken(), "");
    assert.equal(redirectedTo, "/login.html?returnUrl=%2F");
  } finally {
    delete global.fetch;
    delete global.window;
  }
});

test("an authenticated request is not sent without a browser session token", async () => {
  let redirectedTo = "";
  let requestSent = false;
  global.window = {
    sessionStorage: createStorage(),
    location: {
      origin: "https://agent.example.test",
      pathname: "/",
      search: "",
      hash: "",
      replace: value => { redirectedTo = value; }
    }
  };
  global.fetch = async () => {
    requestSent = true;
    return new Response(null, { status: 200 });
  };
  try {
    await assert.rejects(
      authorizedFetch("/api/agents"),
      /login session is required/);
    assert.equal(requestSent, false);
    assert.equal(redirectedTo, "/login.html?returnUrl=%2F");
  } finally {
    delete global.fetch;
    delete global.window;
  }
});
