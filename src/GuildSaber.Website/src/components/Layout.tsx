import SideBar, { SidebarPortalProvider } from "@/components/SideBar"
import { Outlet, ScrollRestoration } from "react-router"

const Layout = () => (
  <SidebarPortalProvider>
    <div className="text-foreground flex min-h-svh w-full">
      <SideBar />

      <div className="flex w-full flex-1 flex-col p-3">
        <Outlet />
      </div>
      <ScrollRestoration />
    </div>
  </SidebarPortalProvider>
)

export default Layout
