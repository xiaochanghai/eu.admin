import { getAccessToken, normalizeReturnUrl, setAccessToken } from "./auth.js";

const form = document.querySelector("#loginForm");
const account = document.querySelector("#userAccount");
const password = document.querySelector("#password");
const submit = document.querySelector("#loginButton");
const errorMessage = document.querySelector("#loginError");

function returnUrl() {
  const value = new URLSearchParams(window.location.search).get("returnUrl") || "/";
  return normalizeReturnUrl(value, window.location.origin);
}

if (getAccessToken()) window.location.replace(returnUrl());

form.addEventListener("submit", async event => {
  event.preventDefault();
  errorMessage.hidden = true;
  submit.disabled = true;
  submit.textContent = "正在登录…";

  try {
    const response = await fetch("/api/session/login", {
      method: "POST",
      headers: { Accept: "application/json", "Content-Type": "application/json" },
      body: JSON.stringify({ UserAccount: account.value.trim(), PassWord: password.value })
    });
    let payload = null;
    try { payload = await response.json(); } catch { /* handled below */ }
    if (!response.ok || payload?.Success !== true || !payload?.Data?.Token) {
      throw new Error(payload?.Message || `登录失败 (${response.status})`);
    }

    setAccessToken(payload.Data.Token);
    password.value = "";
    window.location.replace(returnUrl());
  } catch (error) {
    errorMessage.textContent = error instanceof Error ? error.message : "登录失败，请重试。";
    errorMessage.hidden = false;
    password.focus();
  } finally {
    submit.disabled = false;
    submit.textContent = "登录";
  }
});
