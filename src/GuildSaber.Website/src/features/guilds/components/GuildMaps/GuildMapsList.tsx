import { getRankedMapsOptions } from "@/client/@tanstack/react-query.gen"
import Pagination from "@/components/Pagination"
import { Button } from "@/components/ui/button"
import { Sheet, SheetContent, SheetHeader, SheetTitle, SheetTrigger } from "@/components/ui/sheet"
import { Skeleton } from "@/components/ui/skeleton"
import GuildMapRow from "@/features/guilds/components/GuildMaps/GuildMapRow"
import { GuildMapRowSkeleton } from "@/features/guilds/components/GuildMaps/GuildMapRow/GuildMapRowSkeleton"
import GuildMapsFilters from "@/features/guilds/components/GuildMaps/GuildMapsFilters"
import { useGuildContext } from "@/features/guilds/contexts/guildContext"
import { useGuildMapFilters } from "@/features/guilds/hooks/useGuildMapFilters"
import { useQuery } from "@tanstack/react-query"
import { Filter } from "lucide-react"
import { useMediaQuery } from "usehooks-ts"

const GuildMapsList = () => {
  const guild = useGuildContext()
  const [filters] = useGuildMapFilters()
  const isDesktop = useMediaQuery("(min-width: 64rem)")

  const {
    data: maps,
    isLoading,
    isFetching,
  } = useQuery({
    ...getRankedMapsOptions({
      path: {
        contextId: guild?.contexts[0].id ?? 0,
      },
      query: {
        search: filters.search,
        order: filters.order,
        sortBy: filters.sort,
        difficultyStarFrom: filters.stars[0],
        difficultyStarTo: filters.stars[1],
        bpmFrom: filters.bpm[0],
        bpmTo: filters.bpm[1],
        page: filters.page,
        pageSize: isDesktop ? 16 : 8,
        categoryIds: filters.categories,
        matchAnyCategory: filters.matchAnyCategory,
      },
    }),
    enabled: Boolean(guild),
  })

  if (isLoading || !guild) {
    return <LoadingSkeleton />
  }

  return (
    <div>
      <div className="mb-3 flex items-start justify-between">
        <div>
          <div className="leading-none font-semibold">Ranked Maps</div>
          <div className="text-muted-foreground text-sm">{maps?.totalCount} maps</div>
        </div>

        <Sheet>
          <SheetTrigger asChild>
            <Button size="icon" variant="outline" className="lg:hidden">
              <Filter />
            </Button>
          </SheetTrigger>
          <SheetContent>
            <SheetHeader>
              <SheetTitle>Search Filters</SheetTitle>
            </SheetHeader>
            <div className="overflow-y-auto px-4 pb-4">
              <GuildMapsFilters categories={guild?.categories} />
            </div>
          </SheetContent>
        </Sheet>
      </div>
      <div className="grid grid-cols-1 gap-3 lg:grid-cols-2">
        {maps?.data?.map((map) => (
          <GuildMapRow key={map.id} map={map} />
        ))}
      </div>

      <Pagination totalPages={maps?.totalPages as number} isLoading={isFetching} />
    </div>
  )
}

const LoadingSkeleton = () => {
  const isDesktop = useMediaQuery("(min-width: 64rem)")

  return (
    <div>
      <div className="leading-none font-semibold">Ranked Maps</div>
      <div className="text-muted-foreground flex items-center gap-2 text-sm">
        <Skeleton className="mt-1 h-3 w-18 rounded-sm" />
      </div>

      <div className="grid grid-cols-1 gap-3 lg:grid-cols-2">
        {Array.from({ length: isDesktop ? 16 : 8 }).map((_, i) => (
          <div key={i}>
            <GuildMapRowSkeleton />
          </div>
        ))}
      </div>
    </div>
  )
}

export default GuildMapsList
