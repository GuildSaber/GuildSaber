import { ThemeProvider } from "@/components/Theme"
import { Toaster } from "@/components/ui/sonner"
import { TooltipProvider } from "@/components/ui/tooltip"
import DialogArcViewer from "@/features/maps/components/DialogArcViewer"
import "@/index.css"
import "@/lib/client"
import queryClient from "@/lib/queryClient"
import Router from "@/router"
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
        <TooltipProvider>
          <NuqsAdapter>
            <QueryClientProvider client={queryClient}>
              <Router />
              <DialogArcViewer />
              <Toaster />
            </QueryClientProvider>
          </NuqsAdapter>
        </TooltipProvider>
      </ThemeProvider>
    </StrictMode>,
  )
} else {
  throw new Error("Root element not found")
}
