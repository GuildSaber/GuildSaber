import type { EOrder, ERankedMapLeaderboardSorter } from "@/client"
import { LEADERBOARD_SORT_BY, ORDER_BY } from "@/utils/constants"
import { parseAsInteger, parseAsString, parseAsStringLiteral, useQueryStates } from "nuqs"

const sortKeys = Object.keys(LEADERBOARD_SORT_BY) as ERankedMapLeaderboardSorter[]
const orderKeys = Object.keys(ORDER_BY) as EOrder[]

export const useMapLeaderboardFilters = () =>
  useQueryStates({
    page: parseAsInteger.withDefault(1),
    point: parseAsString.withDefault(""),
    sortBy: parseAsStringLiteral(sortKeys).withDefault(sortKeys[0]),
    order: parseAsStringLiteral(orderKeys).withDefault(orderKeys[0]),
    search: parseAsString.withDefault(""),
  })

export type LeaderboardFilters = ReturnType<typeof useMapLeaderboardFilters>[0]
