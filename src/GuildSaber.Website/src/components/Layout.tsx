import { Outlet, ScrollRestoration } from "react-router"
import Footer from "./Footer"
import Header from "./Header"

const Layout = () => (
  <div className="text-foreground flex min-h-screen w-full flex-col">
    <Header />
    <div className="mx-auto flex w-full max-w-7xl flex-1 flex-col p-3">
      <Outlet />
    </div>
    <Footer />
    <ScrollRestoration />
  </div>
)

export default Layout
