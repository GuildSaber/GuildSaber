import { getGuildExtendedOptions } from "@/client/@tanstack/react-query.gen"
import ErrorState from "@/components/ErrorState"
import GuildHeader from "@/features/guilds/components/GuildHeader"
import GuildMaps from "@/features/guilds/components/GuildMaps"
import { GuildContextProvider } from "@/features/guilds/contexts/guildContext"
import { useQuery } from "@tanstack/react-query"
import { SearchX } from "lucide-react"
import { useParams } from "react-router"

const HomeGuild = () => {
  const { guildId } = useParams()
  const { data: guild, isLoading } = useQuery({
    ...getGuildExtendedOptions({ path: { guildId: guildId as string } }),
  })

  if (!guild && !isLoading) {
    return (
      <ErrorState
        icon={SearchX}
        title="Guild not found"
        variant="warning"
        description="The guild you're looking for doesn't exist or has been removed."
      />
    )
  }

  return (
    <main className="space-y-3">
      <GuildContextProvider guild={guild}>
        <GuildHeader />
        <GuildMaps />
      </GuildContextProvider>
    </main>
  )
}

export default HomeGuild
