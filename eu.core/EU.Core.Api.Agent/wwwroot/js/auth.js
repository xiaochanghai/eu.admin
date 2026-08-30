const accessTokenKey = "eu.agent.access-token";

function storage() {
  return typeof window === "undefined" ? null : window.sessionStorage;
}

export function getAccessToken() {
  return storage()?.getItem(accessTokenKey)?.trim() || "";
}

export function setAccessToken(token) {
  const value = typeof token === "string" ? token.trim() : "";
  if (!value) throw new Error("登录响应中没有有效的访问令牌。");
  storage()?.setItem(accessTokenKey, value);
}

export function clearAccessToken() {
  storage()?.removeItem(accessTokenKey);
}

export function normalizeReturnUrl(value, origin) {
  try {
    const target = new URL(value || "/", origin);
    if (target.origin !== origin || target.pathname === "/login.html") return "/";
    return `${target.pathname}${target.search}${target.hash}`;
  } catch {
    return "/";
  }
}

function currentReturnUrl() {
  if (typeof window === "undefined") return "/";
  const current = `${window.location.pathname}${window.location.search}${window.location.hash}`;
  return normalizeReturnUrl(current, window.location.origin);
}

export function loginUrl(returnUrl = currentReturnUrl()) {
  return `/login.html?returnUrl=${encodeURIComponent(returnUrl)}`;
}

export function redirectToLogin() {
  if (typeof window !== "undefined") window.location.replace(loginUrl());
}

export function requireAuthentication() {
  if (getAccessToken()) return true;
  redirectToLogin();
  return false;
}

export function authorizedHeaders(headers, token = getAccessToken()) {
  const result = new Headers(headers || {});
  if (token) result.set("Authorization", `Bearer ${token}`);
  return result;
}

export async function authorizedFetch(input, init = {}) {
  const token = getAccessToken();
  if (!token && typeof window !== "undefined") {
    redirectToLogin();
    throw new Error("Agent login session is required.");
  }
  const response = await fetch(input, {
    ...init,
    headers: token ? authorizedHeaders(init.headers, token) : init.headers
  });
  if (response.status === 401 && typeof window !== "undefined") {
    clearAccessToken();
    redirectToLogin();
  }
  return response;
}
