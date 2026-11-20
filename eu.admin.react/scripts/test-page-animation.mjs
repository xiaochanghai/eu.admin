import assert from "node:assert/strict";
import fs from "node:fs";
import vm from "node:vm";
import { test } from "node:test";
import ts from "typescript";
import less from "less";

function load(path, modules = {}) {
  const source = fs.readFileSync(new URL(path, import.meta.url), "utf8");
  const code = ts.transpileModule(source, { compilerOptions: { module: ts.ModuleKind.CommonJS } }).outputText;
  const context = { exports: {}, require: id => modules[id] };
  vm.runInNewContext(code, context);
  return context.exports;
}

const animation = load("../src/config/pageAnimation.ts");
const theme = load("../src/config/theme.ts", {
  "@/config": { DEFAULT_PRIMARY: "#1677ff" },
  "./pageAnimation": animation
});

test("old and invalid settings fall back to slide while explicit none survives", () => {
  assert.equal(animation.readPageAnimationMode(undefined), "fade-slide");
  assert.equal(theme.readThemeConfig([]).pageAnimation, "fade-slide");
  assert.equal(
    theme.readThemeConfig([{ ConfigCode: "theme.pageAnimation", ConfigValue: "invalid" }]).pageAnimation,
    "fade-slide"
  );
  assert.equal(animation.readPageAnimationMode("none"), "none");
});

test("all animation options round-trip through the existing theme config format", () => {
  for (const { value } of animation.pageAnimationOptions) {
    const setting = { ...theme.defaultSystemTheme, pageAnimation: value };
    const changes = theme.getThemeConfigChanges({ ID: "test", detail: [] }, setting);
    const item = changes.find(item => item.ConfigCode === "theme.pageAnimation");
    assert.equal(item.InputType, "SELECT");
    assert.equal(theme.readThemeConfig(changes).pageAnimation, value);
    assert.equal(theme.getThemeConfigChanges({ ID: "test", detail: changes }, setting).length, 0);
  }
});

test("animation LESS compiles with explicit disabled selector", async () => {
  const source = fs.readFileSync(new URL("../src/styles/transition.less", import.meta.url), "utf8");
  const { css } = await less.render(source);
  assert.match(css, /:not\(\.page-animation-none\)/);
  assert.match(css, /translateX\(-30px\)/);
});
