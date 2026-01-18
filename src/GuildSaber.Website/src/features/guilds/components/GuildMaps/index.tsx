import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card"
import GuildMapsFilters from "@/features/guilds/components/GuildMaps/GuildMapsFilters"
import GuildMapsList from "@/features/guilds/components/GuildMaps/GuildMapsList"
import { useGuildContext } from "@/features/guilds/contexts/guildContext"
import { useMediaQuery } from "usehooks-ts"

const GuildMaps = () => {
  const guild = useGuildContext()
  const isDesktop = useMediaQuery("(min-width: 64rem)")

  return (
    <section className="relative items-start gap-3 lg:grid lg:grid-cols-[2fr_1fr]">
      <GuildMapsList />

      {isDesktop && (
        <Card className="gap-3!">
          <CardHeader>
            <CardTitle>Search Filters</CardTitle>
          </CardHeader>
          <CardContent>
            <GuildMapsFilters categories={guild?.categories} />
          </CardContent>
        </Card>
      )}
    </section>
  )
}

export default GuildMaps
