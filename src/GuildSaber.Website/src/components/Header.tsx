import { Button } from "@/components/ui/button"
import { useSession } from "@/features/auth/hooks/useSession"
import GuildsSelector from "@/features/guilds/components/GuildSelector"
import { useIsScrolled } from "@/hooks/useIsScrolled"
import { cn } from "@/lib/utils"
import { Link } from "react-router"
import { useMediaQuery } from "usehooks-ts"

const Header = () => {
  const isScrolled = useIsScrolled()
  const isDesktop = useMediaQuery("(min-width: 28rem)")
  const { data: session } = useSession()

  return (
    <header
      className={cn("sticky top-0 z-30 flex h-16 w-full items-center border-b border-transparent", {
        "bg-card/80 border-border border-b backdrop-blur-lg": isScrolled,
      })}
    >
      <div className="mx-auto flex w-full max-w-7xl items-center justify-between px-3">
        <Link to="/">
          <div className="flex items-center gap-3">
            <img src="/gsLogo.svg" alt="GuildSaber" className="h-8 w-8" />
            <h1 className="text-foreground hidden text-xl font-bold md:block">GuildSaber</h1>
          </div>
        </Link>

        {!isDesktop && <GuildsSelector />}

        <section className="flex items-center gap-3">
          {isDesktop && <GuildsSelector />}
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
