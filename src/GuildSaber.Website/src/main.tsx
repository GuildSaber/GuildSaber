import { QueryClientProvider } from "@tanstack/react-query"
import { StrictMode } from "react"
import { createRoot } from "react-dom/client"
import "./index.css"
import queryClient from "./libs/queryClient.ts"
import Router from "./router.tsx"

const rootElement = document.getElementById("root")

if (rootElement) {
  createRoot(rootElement).render(
    <StrictMode>
      <QueryClientProvider client={queryClient}>
        <Router />
      </QueryClientProvider>
    </StrictMode>,
  )
} else {
  throw new Error("Root element not found")
}
