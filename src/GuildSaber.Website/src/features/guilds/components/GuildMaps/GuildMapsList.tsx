import { getRankedMapsOptions } from "@/client/@tanstack/react-query.gen"
import Pagination from "@/components/Pagination"
import { Button } from "@/components/ui/button"
import { Card, CardContent, CardDescription, CardTitle } from "@/components/ui/card"
import { Sheet, SheetContent, SheetHeader, SheetTitle, SheetTrigger } from "@/components/ui/sheet"
import { Skeleton } from "@/components/ui/skeleton"
import GuildMapRow, { GuildMapRowSkeleton } from "@/features/guilds/components/GuildMaps/GuildMapRow/index"
import GuildMapsFilters from "@/features/guilds/components/GuildMaps/GuildMapsFilters"
import { useGuildContext } from "@/features/guilds/contexts/guildContext"
import { useGuildMapFilters } from "@/features/guilds/hooks/useGuildMapFilters"
import { useQuery } from "@tanstack/react-query"
import { Filter } from "lucide-react"

const GuildMapsList = () => {
  const guild = useGuildContext()
  const [filters] = useGuildMapFilters()

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
        categoryIds: filters.categories,
        matchAnyCategory: filters.matchAnyCategory,
      },
    }),
    enabled: Boolean(guild),
  })

  if (isLoading) {
    return <LoadingSkeleton />
  }

  return (
    <Card>
      <CardContent>
        <div className="mb-3 flex items-start justify-between">
          <div>
            <CardTitle>Ranked Maps</CardTitle>
            <CardDescription>{maps?.totalCount} maps</CardDescription>
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
        <div className="divide-y">
          {maps?.data?.map((map) => (
            <GuildMapRow key={map.id} map={map} />
          ))}
        </div>
      </CardContent>

      <Pagination totalPages={maps?.totalPages as number} isLoading={isFetching} />
    </Card>
  )
}

const LoadingSkeleton = () => (
  <Card>
    <CardContent>
      <CardTitle>Ranked Maps</CardTitle>
      <CardDescription className="flex items-center gap-2">
        <Skeleton className="mt-1 h-3 w-18 rounded-sm" />
      </CardDescription>

      <div className="divide-y">
        {Array.from({ length: 10 }).map((_, i) => (
          <div key={i}>
            <GuildMapRowSkeleton />
          </div>
        ))}
      </div>
    </CardContent>
  </Card>
)

export default GuildMapsList
