import { Outlet } from "react-router"

const Layout = () => (
  <main className="flex min-h-screen flex-1 flex-col">
    <Outlet />
  </main>
)

export default Layout
