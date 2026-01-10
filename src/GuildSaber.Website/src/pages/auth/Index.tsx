import { getPlayerExtendedAtMeQueryKey } from "@/client/@tanstack/react-query.gen"
import { client } from "@/client/client.gen"
import JoinCSGuild from "@/features/auth/components/JoinCSGuild"
import DiscordLinkUser from "@/features/auth/components/providers/DiscordLinkProvider"
import SigninOptions from "@/features/auth/components/SigninOptions"
import { useSession } from "@/features/auth/hooks/useSession"
import { useQueryClient } from "@tanstack/react-query"
import { CircleX, LoaderCircle, LogOut } from "lucide-react"
import { useQueryState } from "nuqs"
import { useEffect } from "react"
const Auth = () => {
  const queryClient = useQueryClient()
  const [token, setToken] = useQueryState("token")
  const [error] = useQueryState("error")

  const handleLogout = () => {
    localStorage.removeItem("token")

    client.setConfig({
      headers: {
        Authorization: null,
      },
    })

    queryClient.setQueryData(getPlayerExtendedAtMeQueryKey(), null)
  }

  const { data: session, isLoading, isFetched } = useSession()

  useEffect(() => {
    if (token) {
      localStorage.setItem("token", token)
      setToken(null)

      client.setConfig({
        headers: {
          Authorization: `Bearer ${token}`,
        },
      })
    }
  }, [token, setToken])

  return (
    <div className="flex flex-1 flex-col items-center justify-center">
      <h1 className="mb-3 text-5xl font-extrabold text-blue-500 uppercase italic">GuildSaber</h1>
      {error && (
        <p className="mb-2 flex items-center font-semibold text-red-500">
          <CircleX className="mr-1 inline h-5 w-5" />
          {error}
        </p>
      )}

      {isLoading && <LoaderCircle className="size-11 animate-spin" />}

      {!session?.player && isFetched && <SigninOptions />}

      {session?.player && (
        <div className="flex flex-col justify-center">
          <h2 className="mb-3 text-3xl font-bold">Logged in as {session.player.playerInfo.username}</h2>
          <hr className="my-4 text-slate-700" />
          <JoinCSGuild />

          <hr className="my-4 text-slate-700" />
          <DiscordLinkUser />

          <hr className="my-4 text-slate-700" />
          <button
            onClick={handleLogout}
            className="mx-auto flex w-auto cursor-pointer items-center justify-center gap-3 rounded-md bg-blue-500 fill-white p-2 px-3 text-white transition hover:opacity-80"
          >
            <LogOut className="h-5 w-5" /> Logout
          </button>
        </div>
      )}
    </div>
  )
}

export default Auth
