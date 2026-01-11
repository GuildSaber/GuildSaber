import { Outlet } from "react-router"

const Layout = () => (
  <main className="xl mx-auto flex min-h-screen w-full max-w-360 flex-1 flex-col text-white">
    <Outlet />
  </main>
)

export default Layout
