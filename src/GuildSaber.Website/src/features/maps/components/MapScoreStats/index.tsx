import type { RankedScore } from "@/client"
import { MapScoreStatsBadgeStats } from "@/features/maps/components/MapScoreStats/MapScoreStatsBadgeStats"
import { MapScoreStatsGraphScore } from "@/features/maps/components/MapScoreStats/MapScoreStatsGraphScore"
import { MapScoreStatsHandStats } from "@/features/maps/components/MapScoreStats/MapScoreStatsHandStats"
import { useMapScoreStats } from "@/features/maps/hooks/useMapScoreStats"
import { LEADERBOARD } from "@/utils/constants"
import { AlertCircle } from "lucide-react"

interface Props {
  rankedScore: RankedScore
}

export const MapScoreStats = ({ rankedScore }: Props) => {
  const isBeatLeader = rankedScore.score.type === LEADERBOARD.BeatLeader
  const { data, isLoading, isError } = useMapScoreStats({
    scoreId: Number(rankedScore.score.id),
    enabled: isBeatLeader,
  })

  if (!isBeatLeader) {
    return (
      <div className="bg-muted/40 text-muted-foreground col-span-full rounded-md px-3 py-2 text-center text-xs">
        Statistics are only available for BeatLeader scores.
      </div>
    )
  }

  if (isLoading) {
    return null
  }

  if (isError || !data) {
    return (
      <div className="bg-muted/40 text-muted-foreground col-span-full flex items-center justify-center gap-1.5 rounded-md px-3 py-2 text-xs">
        <AlertCircle className="size-3.5" />
        Failed to load statistics.
      </div>
    )
  }

  return (
    <div className="bg-muted/40 col-span-full flex flex-col gap-3 rounded-md p-3">
      <div className="grid grid-cols-1 gap-4 lg:grid-cols-2">
        <div className="flex flex-col items-center gap-3">
          <MapScoreStatsHandStats accuracyTracker={data.accuracyTracker} />
          <MapScoreStatsBadgeStats
            data={{ winTracker: data.winTracker, hitTracker: data.hitTracker, maxCombo: rankedScore.score.maxCombo }}
          />
        </div>

        <div className="h-48 min-w-0">
          <MapScoreStatsGraphScore data={data} />
        </div>
      </div>
    </div>
  )
}
