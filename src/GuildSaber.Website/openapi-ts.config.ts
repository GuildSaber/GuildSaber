import { defineConfig } from "@hey-api/openapi-ts"
import "dotenv/config"
import path from "node:path"

export default defineConfig({
  input: path.join(import.meta.dirname, "openapi/GuildSaber.Api.json"),
  output: {
    path: "src/client",
    postProcess: ["prettier"],
  },
  plugins: [
    "@hey-api/client-ky",
    {
      name: "@tanstack/react-query",
      queryKeys: {
        tags: true,
      },
    },
  ],
})
