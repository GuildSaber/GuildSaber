import { MAP_SORT_BY, MAX_BPM, MAX_STARS, MIN_BPM, MIN_STARS, ORDER_BY } from "@/utils/constants"
import { parseAsArrayOf, parseAsBoolean, parseAsInteger, parseAsString, useQueryStates } from "nuqs"

export const useGuildMapFilters = () =>
  useQueryStates({
    page: parseAsInteger.withDefault(1),
    search: parseAsString.withDefault(""),
    order: parseAsString.withDefault(Object.keys(ORDER_BY)[0]),
    sort: parseAsString.withDefault(Object.keys(MAP_SORT_BY)[3]),
    stars: parseAsArrayOf(parseAsInteger, ",").withDefault([MIN_STARS, MAX_STARS]),
    bpm: parseAsArrayOf(parseAsInteger, ",").withDefault([MIN_BPM, MAX_BPM]),
    categories: parseAsArrayOf(parseAsString, ",").withDefault([]),
    matchAnyCategory: parseAsBoolean.withDefault(true),
  })
