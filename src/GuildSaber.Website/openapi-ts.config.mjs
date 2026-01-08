import { defineConfig } from "@hey-api/openapi-ts"
import "dotenv/config"
import { env } from "process"

export default defineConfig({
  input: `${env.VITE_API_URL}/openapi/v1.json`,
  output: {
    path: "src/client",
    format: "prettier",
    lint: "eslint",
  },
  plugins: ["@hey-api/client-ky", "@tanstack/react-query"],
})
