import type { EOrder, ERankedMapSorter } from "@/client"
import { getRankedMapsOptions } from "@/client/@tanstack/react-query.gen"
import Pagination from "@/components/Pagination"
import { Button } from "@/components/ui/button"
import { Card, CardContent, CardDescription, CardTitle } from "@/components/ui/card"
import { Sheet, SheetContent, SheetHeader, SheetTitle, SheetTrigger } from "@/components/ui/sheet"
import GuildMapRow from "@/features/guilds/components/GuildMaps/GuildMapRow"
import GuildMapsFilters from "@/features/guilds/components/GuildMaps/GuildMapsFilters"
import { useGuildContext } from "@/features/guilds/contexts/guildContext"
import { useGuildMapFilters } from "@/features/guilds/hooks/useGuildMapFilters"
import { useQuery } from "@tanstack/react-query"
import { Filter, XCircle } from "lucide-react"
import { useMediaQuery } from "usehooks-ts"

const GuildMapsList = () => {
  const guild = useGuildContext()
  const [filters] = useGuildMapFilters()
  const isDesktop = useMediaQuery("(min-width: 64rem)")

  const { data: maps, isLoading } = useQuery({
    ...getRankedMapsOptions({
      path: {
        contextId: guild?.contexts[0].id as number,
      },
      query: {
        search: filters.search as string,
        order: filters.order as EOrder,
        sortBy: filters.sort as ERankedMapSorter,
        difficultyStarFrom: filters.stars[0] as number,
        difficultyStarTo: filters.stars[1] as number,
        bpmFrom: filters.bpm[0] as number,
        bpmTo: filters.bpm[1] as number,
        page: filters.page as number,
        categoryIds: filters.categories as string[],
        matchAnyCategory: filters.matchAnyCategory as boolean,
      },
    }),
  })

  if (!maps && !isLoading) {
    return (
      <Card>
        <CardContent>
          <CardTitle>Ranked Maps</CardTitle>

          <div className="my-8 flex flex-col items-center">
            <XCircle className="text-muted-foreground size-15" />
            <p className="text-foreground mt-2 text-lg">No maps found</p>
          </div>
        </CardContent>
      </Card>
    )
  }

  return (
    <Card>
      <CardContent>
        <div className="mb-3 flex items-start justify-between">
          <div>
            <CardTitle>Ranked Maps</CardTitle>
            <CardDescription>{maps?.totalCount} maps</CardDescription>
          </div>

          {!isDesktop && (
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
          )}
        </div>
        <div className="divide-y">
          {maps?.data?.map((map) => (
            <GuildMapRow key={map.id} map={map} />
          ))}
        </div>
      </CardContent>

      <Pagination totalPages={maps?.totalPages as number} />
    </Card>
  )
}

export default GuildMapsList
