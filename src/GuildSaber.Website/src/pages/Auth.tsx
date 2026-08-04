import { logout } from "@/client"
import { getPlayerExtendedAtMeQueryKey } from "@/client/@tanstack/react-query.gen"
import Flag from "@/components/Flag"
import { Button } from "@/components/ui/button"
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card"
import AuthSetupStepper from "@/features/auth/components/AuthSetupStepper"
import SigninOptions from "@/features/auth/components/SigninOptions"
import { useSession } from "@/features/auth/hooks/useSession"
import { useGuildsStore } from "@/features/guilds/stores/guildsStore"
import { useQueryClient } from "@tanstack/react-query"
import { CircleX, LogOut } from "lucide-react"
import { useQueryState } from "nuqs"
import { Link } from "react-router"

const Auth = () => {
  const queryClient = useQueryClient()
  const [error] = useQueryState("error")
  const clearGuildsStore = useGuildsStore((state) => state.clear)

  const handleLogout = async () => {
    await logout()

    queryClient.setQueryData(getPlayerExtendedAtMeQueryKey(), null)
    clearGuildsStore()
  }

  const { data: session } = useSession()

  return (
    <main className="flex flex-1 flex-col items-center justify-center">
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
            <AuthSetupStepper />
          ) : (
            <>
              <SigninOptions />

              {error && (
                <p className="mt-4 mb-2 flex items-center gap-2 break-all text-red-700">
                  <CircleX className="mr-1 inline h-5 w-5 shrink-0" />
                  {decodeURIComponent(error.replace(/\+/g, " "))}
                </p>
              )}
            </>
          )}
          <p className="text-muted-foreground mt-5 text-xs leading-5">
            GuildSaber processes account and provider data when you sign in or link an account. See the{" "}
            <Link className="text-primary underline underline-offset-4" to="/privacy-policy">
              privacy policy
            </Link>
            .
          </p>
        </CardContent>
      </Card>
    </main>
  )
}

export default Auth
