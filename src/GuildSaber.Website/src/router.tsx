import ErrorBoundary from "@/components/ErrorBoundary"
import Layout from "@/components/Layout"
import Auth from "@/pages/Auth"
import NotFound from "@/pages/NotFound"
import HomeGuild from "@/pages/guilds/Index"
import { createBrowserRouter, Outlet, RouterProvider } from "react-router"

const router = createBrowserRouter([
  {
    path: "/",
    element: <Layout />,
    children: [
      {
        errorElement: <ErrorBoundary />,
        element: <Outlet />,
        children: [
          { index: true, element: <Auth /> },
          { path: "auth", element: <Auth /> },
          {
            path: "guilds",
            children: [{ path: ":guildId", element: <HomeGuild /> }],
          },
          { path: "*", element: <NotFound /> },
        ],
      },
    ],
  },
])

const Router = () => <RouterProvider router={router} />

export default Router
