import type { RankedScoreWithPlayer } from "@/client"
import { Badge } from "@/components/Badge"
import Flag from "@/components/Flag"
import Image from "@/components/Image"
import { Button } from "@/components/ui/button"
import { MapScoreStats } from "@/features/maps/components/MapScoreStats"
import { useMapContext } from "@/features/maps/contexts/mapContext"
import { useMapScoreStats } from "@/features/maps/hooks/useMapScoreStats"
import { useReplayStore } from "@/features/maps/stores/replayStore"
import { formatDate, formatPoints, formatScore } from "@/features/maps/utils"
import { cn } from "@/lib/utils"
import { LEADERBOARD } from "@/utils/constants"
import { ChevronDown, Loader2, Play } from "lucide-react"
import { useState } from "react"
import { useMediaQuery } from "usehooks-ts"

const TYPES_WITH_RANK = ["Valid", "Accepted"] as const
const TYPES_WITH_POINTS = ["Valid", "Accepted", "Pending", "Refused"] as const

const RANK_COLORS: Record<number, string> = {
  1: "text-amber-400",
  2: "text-slate-400",
  3: "text-orange-600",
}

interface Props {
  score: RankedScoreWithPlayer
  pointName: string
}

export const MapLeaderboardRow = ({ score, pointName }: Props) => {
  const { rankedScore, player } = score
  const { openReplay } = useReplayStore()
  const map = useMapContext()
  const maxScore = map?.versions[0]?.difficulty?.stats?.maxScore ?? 0

  const rank = (() => {
    if (!TYPES_WITH_RANK.includes(rankedScore.type as never)) {
      return null
    }

    return (rankedScore as { rank: number }).rank
  })()
  const rawPoints = (() => {
    if (!TYPES_WITH_POINTS.includes(rankedScore.type as never)) {
      return null
    }

    return (rankedScore as { rawPoints: number }).rawPoints
  })()

  const accuracy = (() => {
    if (Number(maxScore) <= 0) {
      return null
    }

    return (Number(rankedScore.effectiveScore) / Number(maxScore)) * 100
  })()

  const isMobile = useMediaQuery("(max-width: 639px)")
  const hasReplay = rankedScore.score.type === LEADERBOARD.BeatLeader && Boolean(rankedScore.score.beatLeaderScoreId)
  const hasStatistics = rankedScore.score.type === LEADERBOARD.BeatLeader && rankedScore.score.hasStatistics

  const handleReplay = () => openReplay(rankedScore.score)

  const [isDetailsOpen, setIsDetailsOpen] = useState(false)
  const toggleDetails = () => setIsDetailsOpen((prev) => !prev)

  const { isLoading: isLoadingDetails } = useMapScoreStats({
    scoreId: Number(rankedScore.score.id),
    enabled: isDetailsOpen,
  })

  return (
    <div className="col-span-full grid grid-cols-[auto_minmax(0,1fr)] items-center gap-x-3 gap-y-2 py-2.5 sm:grid-cols-subgrid">
      <span
        className={cn(
          "text-center text-sm font-semibold tabular-nums",
          rank !== null ? RANK_COLORS[Number(rank)] : "text-muted-foreground",
        )}
      >
        {rank ? `#${rank}` : "–"}
      </span>

      <div className="flex min-w-0 items-center gap-2 sm:contents">
        <div className="flex min-w-0 flex-1 items-center gap-2">
          <Image src={player.playerInfo.avatarUrl} className="size-6 shrink-0 rounded-lg object-cover md:size-8" />
          <Flag code={player.playerInfo.country} className="h-3.5 w-5 shrink-0 object-cover" />
          <span className="truncate font-medium">{player.playerInfo.username}</span>
        </div>

        <span className="text-muted-foreground shrink-0 text-right text-sm tabular-nums">
          {formatDate(rankedScore.score.setAt, isMobile ? "short" : "long")}
        </span>

        <div className="flex items-center justify-end gap-2">
          {hasStatistics && (
            <Button size="xs" variant="ghost" onClick={toggleDetails} aria-expanded={isDetailsOpen}>
              {isDetailsOpen && isLoadingDetails ? (
                <Loader2 className="animate-spin" />
              ) : (
                <ChevronDown className={cn("transition-transform", isDetailsOpen && "rotate-180")} />
              )}
            </Button>
          )}

          {hasReplay && (
            <Button size="xs" variant="outline" onClick={handleReplay}>
              <Play className="size-3" />
            </Button>
          )}
        </div>
      </div>

      <div className="col-span-full mt-1 flex flex-wrap items-center justify-end gap-2 sm:contents">
        {rawPoints !== null ? (
          <Badge className="flex-1 justify-center border-amber-800 bg-amber-800/10 text-amber-900 tabular-nums md:flex-none dark:border-amber-400 dark:bg-amber-400/20 dark:text-amber-400">
            {formatPoints(rawPoints)} {pointName}
          </Badge>
        ) : (
          <span className="text-muted-foreground flex-1 text-center text-xs italic">
            {rankedScore.type?.toLowerCase() ?? "–"}
          </span>
        )}

        <Badge
          className={cn(
            "flex-1 justify-center border-blue-800 bg-blue-800/10 text-blue-900 tabular-nums md:flex-none dark:border-blue-400 dark:bg-blue-400/20 dark:text-blue-400",
            accuracy === null && "hidden sm:invisible sm:flex",
          )}
        >
          {accuracy !== null ? `${accuracy.toFixed(2)}%` : ""}
        </Badge>

        <Badge className="flex-1 justify-center tabular-nums md:flex-none">
          {formatScore(rankedScore.score.baseScore)}
        </Badge>
      </div>

      {isDetailsOpen && <MapScoreStats rankedScore={rankedScore} />}
    </div>
  )
}
