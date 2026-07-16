import Pagination from "@/components/Pagination"
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card"
import { useMapLeaderboard } from "@/features/maps/hooks/useMapLeaderboard"
import { AlertCircle, Logs } from "lucide-react"
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
          <div className="grid grid-cols-[auto_minmax(0,1fr)] gap-x-3 divide-y md:grid-cols-[auto_minmax(0,1fr)_auto_auto_auto_auto]">
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
