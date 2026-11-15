import assert from "node:assert/strict";
import { test } from "node:test";
import { loadConfig } from "./config.js";

test("loadConfig reads PORT", () => {
  const cfg = loadConfig({ PORT: "9000", NODE_ENV: "test" });
  assert.equal(cfg.port, 9000);
  assert.equal(cfg.nodeEnv, "test");
});

test("loadConfig rejects invalid PORT", () => {
  assert.throws(() => loadConfig({ PORT: "nope" }), /Invalid PORT/);
});
