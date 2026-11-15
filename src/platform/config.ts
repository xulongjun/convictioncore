export interface AppConfig {
  nodeEnv: string;
  port: number;
}

export function loadConfig(
  env: NodeJS.ProcessEnv = process.env,
): AppConfig {
  const portRaw = env.PORT ?? "8787";
  const port = Number.parseInt(portRaw, 10);
  if (!Number.isFinite(port) || port <= 0) {
    throw new Error(`Invalid PORT: ${portRaw}`);
  }
  return {
    nodeEnv: env.NODE_ENV ?? "development",
    port,
  };
}
