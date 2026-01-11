import { logout } from "@/client"
import { getPlayerExtendedAtMeQueryKey } from "@/client/@tanstack/react-query.gen"
import { client } from "@/client/client.gen"
import Flag from "@/components/Flag"
import { Button } from "@/components/ui/button"
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card"
import JoinCSGuild from "@/features/auth/components/JoinCSGuild"
import DiscordLinkUser from "@/features/auth/components/providers/DiscordLinkProvider"
import SigninOptions from "@/features/auth/components/SigninOptions"
import { useSession } from "@/features/auth/hooks/useSession"
import { useQueryClient } from "@tanstack/react-query"
import { CircleX, LogOut } from "lucide-react"
import { useQueryState } from "nuqs"
import { useEffect } from "react"

const Auth = () => {
  const queryClient = useQueryClient()
  const [token, setToken] = useQueryState("token")
  const [error] = useQueryState("error")

  const handleLogout = async () => {
    await logout()
    localStorage.removeItem("token")

    client.setConfig({
      headers: {
        Authorization: null,
      },
    })

    queryClient.setQueryData(getPlayerExtendedAtMeQueryKey(), null)
  }

  const { data: session } = useSession()

  useEffect(() => {
    if (token) {
      localStorage.setItem("token", token)
      setToken(null)

      client.setConfig({
        headers: {
          Authorization: `Bearer ${token}`,
        },
      })

      queryClient.invalidateQueries({ queryKey: getPlayerExtendedAtMeQueryKey() })
    }
  }, [token, setToken, queryClient])

  return (
    <main className="flex flex-1 flex-col items-center justify-center">
      <div className="mb-5 flex items-center gap-3">
        <img src="/gsLogo.svg" alt="GuildSaber" className="h-10 w-10" />
        <h1 className="text-foreground text-4xl font-bold">GuildSaber</h1>
      </div>
      <Card className="w-full max-w-md">
        {session ? (
          <CardHeader className="flex items-center justify-between border-b">
            <div className="flex items-center gap-3">
              <img
                src={session.player.playerInfo.avatarUrl}
                alt={session.player.playerInfo.username}
                className="size-14 rounded-full"
              />
              <div>
                <div className="flex items-center gap-2">
                  <Flag code={session.player.playerInfo.country} className="size-6" />
                  <p className="line-clamp-1 text-xl break-all">{session.player.playerInfo.username}</p>
                </div>

                <p>
                  Id:
                  <span className="ml-2 inline rounded bg-gray-800 px-1 font-mono text-white/90">
                    {session.player.id}
                  </span>
                </p>
              </div>
            </div>

            <Button onClick={handleLogout}>
              <LogOut className="h-5 w-5" />
              Logout
            </Button>
          </CardHeader>
        ) : (
          <CardHeader>
            <CardTitle>Sign in</CardTitle>
            <CardDescription>Choose a method to sign in</CardDescription>
          </CardHeader>
        )}
        <CardContent>
          {session ? (
            <>
              <JoinCSGuild />
              <hr className="my-4 text-slate-700" />
              <DiscordLinkUser />
            </>
          ) : (
            <>
              <SigninOptions />

              {error && (
                <p className="mt-4 mb-2 flex items-center text-red-700">
                  <CircleX className="mr-1 inline h-5 w-5" />
                  {decodeURI(error)}
                </p>
              )}
            </>
          )}
        </CardContent>
      </Card>
    </main>
  )
}

export default Auth
