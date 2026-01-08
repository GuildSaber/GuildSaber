import Layout from "@/components/Layout"
import Auth from "@/pages/auth/Index"
import { BrowserRouter, Route, Routes } from "react-router"

const Router = () => (
  <BrowserRouter>
    <Routes>
      <Route path="/" element={<Layout />}>
        <Route index element={<Auth />} />
        <Route path="auth" element={<Auth />} />
      </Route>
    </Routes>
  </BrowserRouter>
)

export default Router
