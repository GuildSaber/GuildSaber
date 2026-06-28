import type { EOrder, ERankedMapLeaderboardSorter, PointLite } from "@/client"
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select"
import { useMapLeaderboardFilters } from "@/features/maps/hooks/useMapLeaderboardFilters"
import { LEADERBOARD_SORT_BY, ORDER_BY } from "@/utils/constants"

interface Props {
  contextPoints: PointLite[]
}

const MapLeaderboardFilters = ({ contextPoints }: Props) => {
  const [filters, setFilters] = useMapLeaderboardFilters()

  const effectivePointId = filters.point || String(contextPoints[0]?.id ?? "")

  const handlePointChange = (v: string) => setFilters({ point: v, page: 1 })
  const handleSortChange = (v: ERankedMapLeaderboardSorter) => setFilters({ sortBy: v, page: 1 })
  const handleOrderChange = (v: EOrder) => setFilters({ order: v, page: 1 })

  return (
    <div className="flex flex-wrap gap-2">
      {contextPoints.length > 1 && (
        <Select value={effectivePointId} onValueChange={handlePointChange}>
          <SelectTrigger size="sm" className="w-auto">
            <SelectValue placeholder="Point" />
          </SelectTrigger>

          <SelectContent>
            {contextPoints.map((point) => (
              <SelectItem key={String(point.id)} value={String(point.id)}>
                {point.name}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
      )}

      <Select value={filters.sortBy} onValueChange={handleSortChange}>
        <SelectTrigger size="sm" className="w-auto">
          <SelectValue />
        </SelectTrigger>

        <SelectContent>
          {Object.entries(LEADERBOARD_SORT_BY).map(([value, label]) => (
            <SelectItem key={value} value={value}>
              {label}
            </SelectItem>
          ))}
        </SelectContent>
      </Select>

      <Select value={filters.order} onValueChange={handleOrderChange}>
        <SelectTrigger size="sm" className="w-auto">
          <SelectValue />
        </SelectTrigger>

        <SelectContent>
          {Object.entries(ORDER_BY).map(([value, label]) => (
            <SelectItem key={value} value={value}>
              {label}
            </SelectItem>
          ))}
        </SelectContent>
      </Select>
    </div>
  )
}

export default MapLeaderboardFilters
