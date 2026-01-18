import { defineConfig } from "@hey-api/openapi-ts"
import "dotenv/config"
import path from "node:path"

export default defineConfig({
  input: path.resolve(__dirname, "../GuildSaber.Api/GuildSaber.Api.json"),
  output: {
    path: "src/client",
    format: "prettier",
    lint: "eslint",
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
