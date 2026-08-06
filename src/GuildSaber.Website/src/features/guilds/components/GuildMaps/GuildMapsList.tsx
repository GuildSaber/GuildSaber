import type { RankedMapWithScores } from "@/client"
import { getRankedMapsOptions, getRankedMapsWithScoresAtMeOptions } from "@/client/@tanstack/react-query.gen"
import Pagination from "@/components/Pagination"
import { Skeleton } from "@/components/ui/skeleton"
import { useSession } from "@/features/auth/hooks/useSession"
import GuildMapRow from "@/features/guilds/components/GuildMaps/GuildMapRow"
import { GuildMapRowSkeleton } from "@/features/guilds/components/GuildMaps/GuildMapRow/GuildMapRowSkeleton"
import { useGuildContext } from "@/features/guilds/contexts/guildContext"
import { useGuildMapFilters } from "@/features/guilds/hooks/useGuildMapFilters"
import { useResponsivePageSize } from "@/hooks/useResponsivePageSize"
import { useQuery } from "@tanstack/react-query"
import { useMediaQuery } from "usehooks-ts"

const GuildMapsList = () => {
  const { data: session } = useSession()
  const guild = useGuildContext()
  const [filters, setFilters] = useGuildMapFilters()
  const hasTwoColumns = useMediaQuery("(min-width: 80rem)")
  const hasCompactRows = useMediaQuery("(max-width: 47.999rem)")
  const { page, pageSize } = useResponsivePageSize({
    page: filters.page,
    setPage: (nextPage) => setFilters({ page: nextPage }),
    isCompact: hasCompactRows,
    columns: hasTwoColumns ? 2 : 1,
  })

  const rankedMapsParams = {
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
      page,
      pageSize,
      categoryIds: filters.categories,
      matchAnyCategory: filters.matchAnyCategory,
    },
  }

  const rankedMapsQuery = useQuery({
    ...getRankedMapsOptions(rankedMapsParams),
    enabled: Boolean(guild) && !session,
  })

  const rankedMapsWithScoresQuery = useQuery({
    ...getRankedMapsWithScoresAtMeOptions(rankedMapsParams),
    enabled: Boolean(guild) && Boolean(session),
  })

  const activeQuery = session ? rankedMapsWithScoresQuery : rankedMapsQuery

  const scores: RankedMapWithScores[] | undefined = session
    ? rankedMapsWithScoresQuery.data?.data
    : rankedMapsQuery.data?.data?.map((rankedMap) => ({ rankedMap, rankedScores: [] }))

  if (activeQuery.isLoading || !guild) {
    return <LoadingSkeleton pageSize={pageSize} />
  }

  return (
    <div>
      <div className="mb-3">
        <div className="leading-none font-semibold">Ranked Maps</div>
        <div className="text-muted-foreground text-sm">{activeQuery.data?.totalCount} maps</div>
      </div>
      <div className="grid grid-cols-1 gap-3 xl:grid-cols-2">
        {scores?.map((score) => (
          <GuildMapRow key={score.rankedMap.id} score={score} />
        ))}
      </div>

      <Pagination totalPages={activeQuery.data?.totalPages as number} isLoading={activeQuery.isFetching} />
    </div>
  )
}

const LoadingSkeleton = ({ pageSize }: { pageSize: number }) => (
  <div>
    <div className="leading-none font-semibold">Ranked Maps</div>
    <div className="text-muted-foreground flex items-center gap-2 text-sm">
      <Skeleton className="mt-1 h-3 w-18 rounded-sm" />
    </div>

    <div className="grid grid-cols-1 gap-3 xl:grid-cols-2">
      {Array.from({ length: pageSize }).map((_, i) => (
        <div key={i}>
          <GuildMapRowSkeleton />
        </div>
      ))}
    </div>
  </div>
)

export default GuildMapsList
