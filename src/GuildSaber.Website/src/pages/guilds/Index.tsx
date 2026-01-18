import { getGuildExtendedOptions } from "@/client/@tanstack/react-query.gen"
import GuildHeader from "@/features/guilds/components/GuildHeader"
import GuildMaps from "@/features/guilds/components/GuildMaps"
import { GuildContextProvider } from "@/features/guilds/contexts/guildContext"
import { useQuery } from "@tanstack/react-query"
import { Frown } from "lucide-react"
import { useParams } from "react-router"

const HomeGuild = () => {
  const { guildId } = useParams()
  const { data: guild, isLoading } = useQuery({
    ...getGuildExtendedOptions({ path: { guildId: guildId as string } }),
  })

  if (!guild && !isLoading) {
    return (
      <div className="my-14 flex flex-col items-center">
        <Frown className="text-muted-foreground size-15" />
        <p className="text-foreground mt-2 text-2xl">Guild not found</p>
      </div>
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
