import type { RankedMapWithScores } from "@/client"
import { getRankedMapsOptions, getRankedMapsWithScoresAtMeOptions } from "@/client/@tanstack/react-query.gen"
import Pagination from "@/components/Pagination"
import { Skeleton } from "@/components/ui/skeleton"
import { useSession } from "@/features/auth/hooks/useSession"
import GuildMapRow from "@/features/guilds/components/GuildMaps/GuildMapRow"
import { GuildMapRowSkeleton } from "@/features/guilds/components/GuildMaps/GuildMapRow/GuildMapRowSkeleton"
import { useGuildContext } from "@/features/guilds/contexts/guildContext"
import { useGuildMapFilters } from "@/features/guilds/hooks/useGuildMapFilters"
import { useQuery } from "@tanstack/react-query"
import { useEffect, useRef } from "react"
import { useMediaQuery } from "usehooks-ts"

const GuildMapsList = () => {
  const { data: session } = useSession()
  const guild = useGuildContext()
  const [filters, setFilters] = useGuildMapFilters()
  const isDesktop = useMediaQuery("(min-width: 96rem)")
  const pageSize = isDesktop ? 16 : 8

  const previousPageSize = useRef(pageSize)

  useEffect(() => {
    if (previousPageSize.current === pageSize) {
      return
    }

    const firstItemIndex = (filters.page - 1) * previousPageSize.current

    previousPageSize.current = pageSize
    setFilters({ page: Math.floor(firstItemIndex / pageSize) + 1 })
  }, [pageSize, filters.page, setFilters])

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
      page: filters.page,
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
    return <LoadingSkeleton />
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

const LoadingSkeleton = () => {
  const isDesktop = useMediaQuery("(min-width: 80rem)")

  return (
    <div>
      <div className="leading-none font-semibold">Ranked Maps</div>
      <div className="text-muted-foreground flex items-center gap-2 text-sm">
        <Skeleton className="mt-1 h-3 w-18 rounded-sm" />
      </div>

      <div className="grid grid-cols-1 gap-3 xl:grid-cols-2">
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
