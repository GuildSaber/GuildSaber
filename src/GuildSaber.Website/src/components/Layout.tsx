import { Outlet } from "react-router"

const Layout = () => (
  <main className="flex min-h-screen flex-1 flex-col bg-slate-900 text-white">
    <Outlet />
  </main>
)

export default Layout
