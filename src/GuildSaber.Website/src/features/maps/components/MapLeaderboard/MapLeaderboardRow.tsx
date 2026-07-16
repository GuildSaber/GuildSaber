import type { RankedScoreWithPlayer } from "@/client"
import { Badge } from "@/components/Badge"
import Flag from "@/components/Flag"
import Image from "@/components/Image"
import { Button } from "@/components/ui/button"
import { Tooltip, TooltipContent, TooltipTrigger } from "@/components/ui/tooltip"
import { MapScoreStats } from "@/features/maps/components/MapScoreStats"
import { useMapContext } from "@/features/maps/contexts/mapContext"
import { useMapScoreStats } from "@/features/maps/hooks/useMapScoreStats"
import { useReplayStore } from "@/features/maps/stores/replayStore"
import { formatDate, formatPoints, formatScore } from "@/features/maps/utils"
import { cn } from "@/lib/utils"
import { LEADERBOARD } from "@/utils/constants"
import { getModifierByLong } from "@/utils/modifiers"
import { formatTime } from "@/utils/time"
import { ChevronDown, Loader2, Pause, Play, X } from "lucide-react"
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
  const beatLeaderScore = rankedScore.score.type === LEADERBOARD.BeatLeader ? rankedScore.score : undefined

  const hasReplay = Boolean(beatLeaderScore?.beatLeaderScoreId)
  const hasStatistics = Boolean(beatLeaderScore?.hasStatistics)
  const pauseCount = beatLeaderScore?.pauseCount ?? null
  const hasPause = Boolean(pauseCount)
  const totalPauseDuration = beatLeaderScore?.totalPauseDuration ?? null
  const { isFullCombo, missedNotes, badCuts } = rankedScore.score
  const missCount = Number(missedNotes) + Number(badCuts)

  const handleReplay = () => openReplay(rankedScore.score)

  const [isDetailsOpen, setIsDetailsOpen] = useState(false)
  const toggleDetails = () => setIsDetailsOpen((prev) => !prev)

  const { isLoading: isLoadingDetails } = useMapScoreStats({
    scoreId: Number(rankedScore.score.id),
    enabled: isDetailsOpen,
  })

  return (
    <div className="col-span-full grid grid-cols-[auto_minmax(0,1fr)] items-center gap-x-3 gap-y-2 py-2.5 md:grid-cols-subgrid">
      <span
        className={cn(
          "text-center text-sm font-semibold tabular-nums",
          rank !== null ? RANK_COLORS[Number(rank)] : "text-muted-foreground",
        )}
      >
        {rank ? `#${rank}` : "–"}
      </span>

      <div className="flex min-w-0 items-center gap-2 md:contents">
        <div className="flex min-w-0 flex-1 items-center gap-2">
          <Image src={player.playerInfo.avatarUrl} className="size-6 shrink-0 rounded-lg object-cover md:size-8" />
          <Flag code={player.playerInfo.country} className="h-3.5 w-5 shrink-0 object-cover" />
          <span className="truncate font-medium">{player.playerInfo.username}</span>
        </div>

        <div className="flex items-center justify-end gap-3 text-right md:flex-col-reverse md:items-end md:gap-1">
          {hasPause && (
            <Tooltip>
              <TooltipTrigger asChild>
                <span className="hidden items-center justify-center text-right text-sm text-red-900 tabular-nums sm:flex dark:text-red-400">
                  {formatTime(Number(totalPauseDuration))}
                  <Pause className="ml-1 size-3" />
                </span>
              </TooltipTrigger>
              <TooltipContent>{pauseCount ?? "–"} pauses</TooltipContent>
            </Tooltip>
          )}
          <span className="text-muted-foreground shrink-0 text-right text-sm tabular-nums">
            {formatDate(rankedScore.score.setAt, isMobile ? "short" : "long")}
          </span>
        </div>

        <div className="flex items-center justify-end gap-3 md:flex-col-reverse">
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

      <div className="col-span-full mt-1 grid grid-cols-2 gap-2 md:col-span-2 md:mt-0">
        {rawPoints !== null ? (
          <Badge className="justify-center border-amber-800 bg-amber-800/10 text-amber-900 tabular-nums dark:border-amber-400 dark:bg-amber-400/20 dark:text-amber-400">
            {formatPoints(rawPoints)} {pointName}
          </Badge>
        ) : (
          <span className="text-muted-foreground text-center text-xs italic">
            {rankedScore.type?.toLowerCase() ?? "–"}
          </span>
        )}

        <Badge
          className={cn(
            "justify-center border-blue-800 bg-blue-800/10 text-blue-900 tabular-nums dark:border-blue-400 dark:bg-blue-400/20 dark:text-blue-400",
            accuracy === null && "hidden sm:invisible sm:flex",
          )}
        >
          {accuracy !== null ? `${accuracy.toFixed(2)}%` : ""}
        </Badge>

        <Badge className="justify-center gap-1 tabular-nums">
          {formatScore(rankedScore.score.baseScore)}
          {rankedScore.score.modifiers && rankedScore.score.modifiers.toLowerCase() !== "none" && (
            <span className="text-muted-foreground self-end text-xs">
              {rankedScore.score.modifiers
                .split(",")
                .map((m) => getModifierByLong(m.trim()))
                .join(", ")}
            </span>
          )}
        </Badge>

        <div className="flex gap-2 sm:gap-3">
          {hasPause && (
            <Tooltip>
              <TooltipTrigger asChild>
                <Badge className="flex-1 justify-center border-red-800 bg-red-800/10 text-red-900 tabular-nums sm:hidden dark:border-red-400 dark:bg-red-400/20 dark:text-red-400">
                  {formatTime(Number(totalPauseDuration))}
                  <Pause className="ml-1 size-3" />
                </Badge>
              </TooltipTrigger>
              <TooltipContent>{pauseCount ?? "–"} pause</TooltipContent>
            </Tooltip>
          )}

          <Badge
            className={cn(
              "flex-1 justify-center tabular-nums",
              isFullCombo
                ? "border-green-800 bg-green-800/10 text-green-900 dark:border-green-400 dark:bg-green-400/20 dark:text-green-400"
                : "border-red-800 bg-red-800/10 text-red-900 dark:border-red-400 dark:bg-red-400/20 dark:text-red-400",
            )}
          >
            {isFullCombo ? (
              "FC"
            ) : (
              <>
                <X className="size-4" />
                {missCount}
              </>
            )}
          </Badge>
        </div>
      </div>

      {isDetailsOpen && <MapScoreStats rankedScore={rankedScore} />}
    </div>
  )
}
