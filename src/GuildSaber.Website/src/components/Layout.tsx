import { Outlet } from "react-router"
import Header from "./Header"

const Layout = () => (
  <div className="text-foreground flex min-h-screen w-full flex-col">
    <Header />
    <div className="mx-auto flex w-full max-w-360 flex-1 flex-col p-3">
      <Outlet />
    </div>
  </div>
)

export default Layout
