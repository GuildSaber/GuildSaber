import { SidebarPortal } from "@/components/SideBar"
import GuildMapsFilters from "@/features/guilds/components/GuildMaps/GuildMapsFilters"
import GuildMapsList from "@/features/guilds/components/GuildMaps/GuildMapsList"
import { useGuildContext } from "@/features/guilds/contexts/guildContext"
import { useMediaQuery } from "usehooks-ts"

const GuildMaps = () => {
  const guild = useGuildContext()
  const isDesktop = useMediaQuery("(min-width: 64rem)")

  return (
    <section className="relative">
      <GuildMapsList />

      {isDesktop && (
        <SidebarPortal>
          <GuildMapsFilters categories={guild?.categories} />
        </SidebarPortal>
      )}
    </section>
  )
}

export default GuildMaps
