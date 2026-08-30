import {
  authorizedFetch,
  clearAccessToken,
  redirectToLogin,
  requireAuthentication
} from "./auth.js";

if (requireAuthentication()) {
  document.body.classList.remove("auth-pending");
  document.querySelector("#logoutButton")?.addEventListener("click", async () => {
    try {
      await authorizedFetch("/api/session/logout", { method: "POST" });
    } finally {
      clearAccessToken();
      redirectToLogin();
    }
  });

  await import("./app.js");
}
