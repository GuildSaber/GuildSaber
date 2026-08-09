import { SidebarPortal } from "@/components/SideBar"
import GuildMapsFilters from "@/features/guilds/components/GuildMaps/GuildMapsFilters"
import GuildMapsList from "@/features/guilds/components/GuildMaps/GuildMapsList"
import { useGuildContext } from "@/features/guilds/contexts/guildContext"
import { Filter } from "lucide-react"

const GuildMaps = () => {
  const guild = useGuildContext()

  return (
    <section className="relative">
      <GuildMapsList />

      <SidebarPortal title="Filters" icon={Filter}>
        <GuildMapsFilters categories={guild?.categories} />
      </SidebarPortal>
    </section>
  )
}

export default GuildMaps
