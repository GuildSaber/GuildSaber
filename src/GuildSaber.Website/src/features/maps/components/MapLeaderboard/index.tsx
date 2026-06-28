import Pagination from "@/components/Pagination"
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card"
import { useMapLeaderboard } from "@/features/maps/hooks/useMapLeaderboard"
import MapLeaderboardFilters from "./MapLeaderboardFilters"
import { MapLeaderboardRow } from "./MapLeaderboardRow"
import { MapLeaderboardSkeleton } from "./MapLeaderboardSkeleton"

const MapLeaderboard = () => {
  const { contextPoints, effectivePointName, leaderboard, isLoading, isFetching } = useMapLeaderboard()

  const hasNoPoints = !isLoading && contextPoints.length === 0
  const isReady = !isLoading && contextPoints.length > 0
  const hasScores = isReady && (leaderboard?.data?.length ?? 0) > 0

  return (
    <Card>
      <CardHeader className="flex flex-row flex-wrap justify-between gap-2">
        <div>
          <CardTitle>Leaderboard</CardTitle>
          {leaderboard?.totalCount !== undefined && (
            <CardDescription>{String(leaderboard.totalCount)} scores</CardDescription>
          )}
        </div>

        <MapLeaderboardFilters contextPoints={contextPoints} />
      </CardHeader>

      <CardContent>
        {isLoading && <MapLeaderboardSkeleton />}

        {hasNoPoints && (
          <p className="text-muted-foreground py-8 text-center text-sm">No points available for this map.</p>
        )}

        {isReady && !hasScores && <p className="text-muted-foreground py-8 text-center text-sm">No scores yet.</p>}

        {hasScores && (
          <div className="grid grid-cols-[auto_minmax(0,1fr)] gap-x-3 divide-y sm:grid-cols-[auto_minmax(0,1fr)_auto_auto_auto_auto_auto]">
            {leaderboard?.data?.map((score) => (
              <MapLeaderboardRow key={score.rankedScore.id} score={score} pointName={effectivePointName} />
            ))}
          </div>
        )}
      </CardContent>

      <Pagination totalPages={Number(leaderboard?.totalPages ?? 1)} isLoading={isFetching} />
    </Card>
  )
}

export default MapLeaderboard
