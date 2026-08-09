import Pagination from "@/components/Pagination"
import MapLeaderboardFilters from "@/features/maps/components/MapLeaderboard/MapLeaderboardFilters"
import { MapLeaderboardRow } from "@/features/maps/components/MapLeaderboard/MapLeaderboardRow"
import { MapLeaderboardSkeleton } from "@/features/maps/components/MapLeaderboard/MapLeaderboardSkeleton"
import { useMapLeaderboard } from "@/features/maps/hooks/useMapLeaderboard"
import { AlertCircle, Logs } from "lucide-react"

const MapLeaderboard = () => {
  const { contextPoints, effectivePointName, leaderboard, isLoading, isFetching } = useMapLeaderboard()

  const hasNoPoints = !isLoading && contextPoints.length === 0
  const isReady = !isLoading && contextPoints.length > 0
  const hasScores = isReady && (leaderboard?.data?.length ?? 0) > 0

  return (
    <div>
      <div className="mb-3 flex flex-row flex-wrap justify-between gap-2">
        <div>
          <div className="leading-none font-semibold">Leaderboard</div>
          {leaderboard?.totalCount !== undefined && (
            <div className="text-muted-foreground text-sm">{String(leaderboard.totalCount)} scores</div>
          )}
        </div>

        <MapLeaderboardFilters contextPoints={contextPoints} />
      </div>

      {!isReady && <MapLeaderboardSkeleton />}

      {isReady && hasNoPoints && (
        <div className="text-muted-foreground flex flex-col items-center gap-2 py-10">
          <AlertCircle className="size-8 opacity-40" />
          <p className="text-sm font-medium">No ranking available</p>
          <p className="text-xs opacity-60">This map hasn't been assigned to any point category yet.</p>
        </div>
      )}

      {isReady && !hasScores && (
        <div className="text-muted-foreground flex flex-col items-center gap-2 py-10">
          <Logs className="size-8 opacity-40" />
          <p className="text-sm font-medium">No scores recorded</p>
          <p className="text-xs opacity-60">Be the first to set a score on this map!</p>
        </div>
      )}

      {hasScores && (
        <div className="flex flex-col gap-3 md:grid md:grid-cols-[auto_minmax(0,1fr)_auto_auto_auto_auto] md:items-start md:gap-x-3 md:gap-y-3">
          {leaderboard?.data?.map((score) => (
            <MapLeaderboardRow key={score.rankedScore.id} score={score} pointName={effectivePointName} />
          ))}
        </div>
      )}

      <Pagination totalPages={Number(leaderboard?.totalPages ?? 1)} isLoading={isFetching} />
    </div>
  )
}

export default MapLeaderboard
