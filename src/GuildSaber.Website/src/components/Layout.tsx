
import Footer from "@/components/Footer"
import Header from "@/components/Header"
import { Outlet } from "react-router"

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
