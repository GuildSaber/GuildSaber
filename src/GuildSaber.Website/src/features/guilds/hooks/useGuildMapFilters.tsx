import type { EOrder, ERankedMapSorter } from "@/client"
import { MAP_SORT_BY, MAX_BPM, MAX_STARS, MIN_BPM, MIN_STARS, ORDER_BY } from "@/utils/constants"
import { parseAsArrayOf, parseAsBoolean, parseAsInteger, parseAsString, parseAsStringLiteral, useQueryStates } from "nuqs"

const orderKeys = Object.keys(ORDER_BY) as EOrder[]
const sortKeys = Object.keys(MAP_SORT_BY) as ERankedMapSorter[]

export const useGuildMapFilters = () =>
  useQueryStates({
    page: parseAsInteger.withDefault(1),
    search: parseAsString.withDefault(""),
    order: parseAsStringLiteral(orderKeys).withDefault(orderKeys[0]),
    sort: parseAsStringLiteral(sortKeys).withDefault(sortKeys[3]),
    stars: parseAsArrayOf(parseAsInteger, ",").withDefault([MIN_STARS, MAX_STARS]),
    bpm: parseAsArrayOf(parseAsInteger, ",").withDefault([MIN_BPM, MAX_BPM]),
    categories: parseAsArrayOf(parseAsInteger, ",").withDefault([]),
    matchAnyCategory: parseAsBoolean.withDefault(true),
  })

export type GuildMapFilters = ReturnType<typeof useGuildMapFilters>[0]
