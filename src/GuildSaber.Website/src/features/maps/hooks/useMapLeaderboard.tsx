import { getContextPointRankedMapLeaderboardOptions, getGuildExtendedOptions } from "@/client/@tanstack/react-query.gen"
import { useMapContext } from "@/features/maps/contexts/mapContext"
import { useMapLeaderboardFilters } from "@/features/maps/hooks/useMapLeaderboardFilters"
import { useQuery } from "@tanstack/react-query"

export const useMapLeaderboard = () => {
  const map = useMapContext()
  const [filters] = useMapLeaderboardFilters()

  const { data: guild, isLoading: isGuildLoading } = useQuery({
    ...getGuildExtendedOptions({ path: { guildId: String(map?.guildId ?? "") } }),
    enabled: Boolean(map?.guildId),
  })

  const contextPoints = guild?.pointsLite ?? []
  const effectivePointId = filters.point || String(contextPoints[0]?.id ?? "")
  const effectivePointName = contextPoints.find((p) => String(p.id) === effectivePointId)?.name ?? "pts"

  const canFetch = Boolean(map?.contextId && map?.id && effectivePointId)

  const {
    data: leaderboard,
    isPending,
    isFetching,
  } = useQuery({
    ...getContextPointRankedMapLeaderboardOptions({
      path: { contextId: String(map?.contextId), pointId: effectivePointId, rankedMapId: String(map?.id) },
      query: {
        page: filters.page,
        sortBy: filters.sortBy,
        order: filters.order,
        search: filters.search,
      },
    }),
    enabled: canFetch,
  })

  return {
    contextPoints,
    effectivePointName,
    leaderboard,
    isLoading: isGuildLoading || (canFetch && isPending),
    isFetching,
  }
}
