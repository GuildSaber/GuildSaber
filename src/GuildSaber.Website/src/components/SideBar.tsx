import Image from "@/components/Image"
import { Separator } from "@/components/ui/separator"
import { Sheet, SheetContent, SheetHeader, SheetTitle, SheetTrigger } from "@/components/ui/sheet"
import { useSession } from "@/features/auth/hooks/useSession"
import GuildsSelector from "@/features/guilds/components/GuildSelector"
import GuildsSelectorMobile from "@/features/guilds/components/GuildSelector/GuildsSelectorMobile"
import { useGuildsStore } from "@/features/guilds/stores/guildsStore"
import { cn } from "@/lib/utils"
import { getCdnUrl } from "@/utils/url"
import { Compass, GripVertical, LogIn, LucideIcon, Package } from "lucide-react"
import { createContext, useCallback, useContext, useEffect, useState, type ReactNode } from "react"
import { createPortal } from "react-dom"
import { Link } from "react-router"
import { useMediaQuery } from "usehooks-ts"

interface SidebarPortalMeta {
  title?: ReactNode
  icon: LucideIcon
}

const SidebarPortalContext = createContext<{
  container: HTMLDivElement | null
  setContainer: (node: HTMLDivElement | null) => void
  hasContent: boolean
  meta: SidebarPortalMeta | null
  registerContent: (meta: SidebarPortalMeta) => () => void
} | null>(null)

export const SidebarPortalProvider = ({ children }: { children: ReactNode }) => {
  const [container, setContainer] = useState<HTMLDivElement | null>(null)
  const [contentCount, setContentCount] = useState(0)
  const [meta, setMeta] = useState<SidebarPortalMeta | null>(null)

  const registerContent = useCallback((newMeta: SidebarPortalMeta) => {
    setContentCount((count) => count + 1)
    setMeta(newMeta)

    return () => {
      setContentCount((count) => count - 1)
      setMeta(null)
    }
  }, [])

  return (
    <SidebarPortalContext.Provider
      value={{ container, setContainer, hasContent: contentCount > 0, meta, registerContent }}
    >
      {children}
    </SidebarPortalContext.Provider>
  )
}

export const SidebarPortal = ({
  children,
  title,
  icon = GripVertical,
}: { children: ReactNode } & Partial<SidebarPortalMeta>) => {
  const ctx = useContext(SidebarPortalContext)
  const registerContent = ctx?.registerContent

  useEffect(() => registerContent?.({ title, icon }), [registerContent, title, icon])

  if (!ctx?.container) {
    return null
  }

  return createPortal(children, ctx.container)
}

interface SidebarLinkProps {
  to: string
  title: ReactNode
  icon?: LucideIcon
  className?: string
  newTab?: boolean
  onClick?: () => void
  disabled?: boolean
}

const SidebarLink = ({ to, title, icon: Icon, className, newTab, onClick, disabled }: SidebarLinkProps) => (
  <Link
    to={to}
    target={newTab ? "_blank" : "_self"}
    rel={newTab ? "noopener noreferrer" : undefined}
    aria-disabled={disabled}
    onClick={onClick}
    className={cn(
      "flex items-center gap-3 rounded-lg px-1 py-2 hover:backdrop-brightness-120",
      disabled && "pointer-events-none opacity-50",
      className,
    )}
  >
    {Icon && <Icon className="size-5" />}
    <p>{title}</p>
  </Link>
)

interface SidebarUserLinkProps {
  className?: string
  onClick?: () => void
}

const SidebarUserLink = ({ className, onClick }: SidebarUserLinkProps) => {
  const { data: session } = useSession()

  return (
    <Link to="/auth" className={className} onClick={onClick}>
      {session ? (
        <div className="flex items-center gap-3 rounded-lg p-1 hover:backdrop-brightness-120">
          <img className="size-9 rounded-lg" src={session.player.playerInfo.avatarUrl} />
          <p className="line-clamp-1 break-all">{session.player.playerInfo.username}</p>
        </div>
      ) : (
        <SidebarLink to="/auth" title="Sign in" icon={LogIn} className="bg-primary px-2" />
      )}
    </Link>
  )
}

const SideBar = () => {
  const { data: session } = useSession()
  const ctx = useContext(SidebarPortalContext)
  const { selectedGuild } = useGuildsStore()
  const [menuOpen, setMenuOpen] = useState(false)
  const [filtersOpen, setFiltersOpen] = useState(false)
  const isDesktop = useMediaQuery("(min-width: 64rem)")

  const closeMenu = () => {
    setMenuOpen(false)
  }

  const hasGuilds = session?.members.length

  return (
    <>
      <div className="sticky top-0 hidden max-h-svh w-84 flex-col gap-3 p-3 lg:flex">
        <div className="flex flex-1 flex-col gap-1 overflow-y-auto p-3">
          <Link to="/" className="mb-3">
            <div className="flex items-center gap-3">
              <img src="/gsLogo.svg" alt="GuildSaber" className="h-8 w-8" />
              <h1 className="text-foreground hidden text-xl font-bold md:block">GuildSaber</h1>
            </div>
          </Link>

          {session && hasGuilds ? (
            <GuildsSelector />
          ) : (
            <SidebarLink to="/guilds" title="Discover Guilds" disabled icon={Compass} />
          )}

          <SidebarLink
            to="https://github.com/GuildSaber/GuildSaber/releases/latest"
            title="Get PC Mod"
            icon={Package}
            newTab
          />

          <SidebarUserLink className="mt-auto" />

          <div className="text-muted-foreground mt-1 flex gap-2 text-center text-xs">
            <Link to="https://github.com/GuildSaber/GuildSaber" className="hover:underline">
              Source Code
            </Link>
            <Link to="/privacy-policy" className="hover:underline">
              Privacy Policy
            </Link>
          </div>
        </div>

        {isDesktop && ctx?.hasContent && (
          <>
            <Separator className="h-0.5!" />

            <div ref={ctx?.setContainer} className="flex-2 overflow-y-auto rounded-xl p-3" />
          </>
        )}
      </div>

      <div
        className={cn(
          "fixed inset-0 z-40 bg-black/50 transition-opacity duration-300 lg:hidden",
          menuOpen ? "opacity-100" : "pointer-events-none opacity-0",
        )}
        onClick={closeMenu}
        aria-hidden="true"
      />

      <button
        onClick={() => setMenuOpen(true)}
        aria-label="Open menu"
        className="bg-background fixed right-4 bottom-6 z-40 size-11 cursor-pointer rounded-lg border p-1 md:bottom-3 lg:hidden"
      >
        <Image
          src={selectedGuild ? getCdnUrl(`guilds/${selectedGuild}/logo.jpg`) : "/gsLogo.svg"}
          className={cn("size-full rounded-md", !selectedGuild && "mx-auto size-8")}
        />
      </button>

      {!isDesktop && ctx?.hasContent && ctx.meta && (
        <Sheet open={filtersOpen} onOpenChange={setFiltersOpen}>
          <SheetTrigger asChild>
            <button
              aria-label={ctx.meta.title ? `Open ${ctx.meta.title}` : "Open panel"}
              className="bg-background/80 fixed top-1/2 right-0 z-40 -translate-y-1/2 rounded-l-lg border border-r-0 p-2 py-6 pr-3 shadow-lg lg:hidden"
            >
              <ctx.meta.icon className="size-5" />
            </button>
          </SheetTrigger>
          <SheetContent>
            <SheetHeader>
              <SheetTitle>{ctx.meta.title}</SheetTitle>
            </SheetHeader>
            <div ref={ctx?.setContainer} className="overflow-y-auto px-4 pb-4" />
          </SheetContent>
        </Sheet>
      )}

      <div
        className={cn(
          "bg-background fixed right-4 bottom-6 z-50 overflow-hidden border shadow-lg transition-[width,border-radius,opacity] duration-300 ease-in-out md:bottom-3 lg:hidden",
          menuOpen
            ? "w-[calc(100%-2rem)] rounded-2xl rounded-br-md opacity-100"
            : "pointer-events-none w-11 rounded-lg opacity-0",
        )}
      >
        <div
          className={cn(
            "grid transition-[grid-template-rows] duration-300 ease-in-out",
            menuOpen ? "grid-rows-[1fr]" : "grid-rows-[0fr]",
          )}
        >
          <div className="min-h-0 overflow-hidden">
            <div className="flex max-h-[calc(100dvh-2.5rem)] flex-col gap-1 overflow-y-auto p-3">
              <SidebarUserLink className="w-full" onClick={closeMenu} />

              <SidebarLink to="/guilds" title="Discover Guilds" disabled icon={Compass} onClick={closeMenu} />

              <SidebarLink
                to="https://github.com/GuildSaber/GuildSaber/releases/latest"
                title="Get PC Mod"
                icon={Package}
                newTab
              />
              <div className="my-4">
                <GuildsSelectorMobile onSelect={closeMenu} />
              </div>

              <div className="text-muted-foreground mt-1 flex gap-2 text-center text-xs">
                <Link to="https://github.com/GuildSaber/GuildSaber" className="hover:underline" onClick={closeMenu}>
                  Source Code
                </Link>
                <Link to="/privacy-policy" className="hover:underline" onClick={closeMenu}>
                  Privacy Policy
                </Link>
              </div>
            </div>
          </div>
        </div>
      </div>
    </>
  )
}

export default SideBar
