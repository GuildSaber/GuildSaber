import Layout from "@/components/Layout"
import Auth from "@/pages/Auth"
import HomeGuild from "@/pages/guilds/Index"
import { BrowserRouter, Route, Routes } from "react-router"

const Router = () => (
  <BrowserRouter>
    <Routes>
      <Route path="/" element={<Layout />}>
        <Route index element={<Auth />} />
        <Route path="auth" element={<Auth />} />

        <Route path="/guilds">
          <Route path=":guildId" element={<HomeGuild />} />
        </Route>
      </Route>
    </Routes>
  </BrowserRouter>
)

export default Router
