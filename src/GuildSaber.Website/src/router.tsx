import { BrowserRouter, Route, Routes } from "react-router"
import Layout from "./components/Layout"
import Home from "./pages/Home"

const Router = () => (
  <BrowserRouter>
    <Routes>
      <Route path="/" element={<Layout />}>
        <Route index element={<Home />} />
      </Route>
    </Routes>
  </BrowserRouter>
)

export default Router
