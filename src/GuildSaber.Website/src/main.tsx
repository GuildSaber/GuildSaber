import { ThemeProvider } from "@/components/Theme"
import "@/index.css"
import "@/lib/client.ts"
import queryClient from "@/lib/queryClient.ts"
import Router from "@/router.tsx"
import "@fontsource-variable/jetbrains-mono"
import "@fontsource-variable/lexend"
import { QueryClientProvider } from "@tanstack/react-query"
import { NuqsAdapter } from "nuqs/adapters/react-router/v7"
import { StrictMode } from "react"
import { createRoot } from "react-dom/client"

const rootElement = document.getElementById("root")

if (rootElement) {
  createRoot(rootElement).render(
    <StrictMode>
      <ThemeProvider defaultTheme="dark">
        <NuqsAdapter>
          <QueryClientProvider client={queryClient}>
            <Router />
          </QueryClientProvider>
        </NuqsAdapter>
      </ThemeProvider>
    </StrictMode>,
  )
} else {
  throw new Error("Root element not found")
}
