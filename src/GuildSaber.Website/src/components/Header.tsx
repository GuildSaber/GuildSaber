import { Button } from "@/components/ui/button"
import { useSession } from "@/features/auth/hooks/useSession"
import GuildsSelector from "@/features/guilds/components/GuildSelector"
import { Link } from "react-router"

const Header = () => {
  const { data: session } = useSession()

  return (
    <header className="bg-card/80 sticky top-0 z-30 flex h-16 w-full items-center border-b backdrop-blur-lg">
      <div className="mx-auto flex w-full max-w-360 items-center justify-between px-3">
        <Link to="/">
          <div className="flex items-center gap-3">
            <img src="/gsLogo.svg" alt="GuildSaber" className="h-8 w-8" />
            <h1 className="text-foreground hidden text-xl font-bold md:block">GuildSaber</h1>
          </div>
        </Link>

        <section className="flex items-center gap-3">
          <GuildsSelector />
          <Link to="/auth">
            {session ? (
              <img className="size-9 rounded-lg" src={session.player.playerInfo.avatarUrl} />
            ) : (
              <Button>Sign in</Button>
            )}
          </Link>
        </section>
      </div>
    </header>
  )
}
export default Header
