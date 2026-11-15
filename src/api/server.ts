import { createServer } from "node:http";
import { loadConfig } from "../platform/config.js";

const config = loadConfig();

const server = createServer((req, res) => {
  if (req.url === "/health" && req.method === "GET") {
    res.writeHead(200, { "content-type": "application/json" });
    res.end(JSON.stringify({ ok: true, service: "convictioncore" }));
    return;
  }
  res.writeHead(404, { "content-type": "application/json" });
  res.end(JSON.stringify({ error: "not_found" }));
});

server.listen(config.port, () => {
  console.log(
    `convictioncore scaffold listening on :${config.port} (${config.nodeEnv})`,
  );
});
