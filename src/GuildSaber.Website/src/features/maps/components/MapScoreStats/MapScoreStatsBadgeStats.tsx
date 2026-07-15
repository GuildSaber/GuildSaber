import type { HitTracker, WinTracker } from "@/client"
import { Badge } from "@/components/Badge"
import { cn } from "@/lib/utils"
import { formatTime } from "@/utils/time"
import type { ReactNode } from "react"

const RED = "text-red-600 dark:text-red-400"
const BLUE = "text-blue-600 dark:text-blue-400"

interface Props {
  data: {
    winTracker: WinTracker
    hitTracker: HitTracker
    maxCombo: number | string | null
  }
}

export const MapScoreStatsBadgeStats = ({ data }: Props) => {
  const { winTracker, hitTracker, maxCombo } = data

  const totals = {
    leftMistakes: Number(hitTracker.leftMiss) + Number(hitTracker.leftBadCuts),
    rightMistakes: Number(hitTracker.rightMiss) + Number(hitTracker.rightBadCuts),
    missedNotes: Number(hitTracker.leftMiss) + Number(hitTracker.rightMiss),
    badCuts: Number(hitTracker.leftBadCuts) + Number(hitTracker.rightBadCuts),
    bombs: Number(hitTracker.leftBombs) + Number(hitTracker.rightBombs),
    accuracy: (Number(winTracker.totalScore) / Number(winTracker.maxScore)) * 100,
  }

  return (
    <div className="flex flex-wrap justify-center gap-1.5">
      <StatPill
        stat={{
          label: "Total mistakes",
          value: totals.leftMistakes + totals.rightMistakes,
          parts: [
            { value: totals.leftMistakes, colorClass: RED },
            { value: totals.rightMistakes, colorClass: BLUE },
          ],
        }}
      />

      <StatPill stat={{ label: "Missed notes", value: totals.missedNotes }} />

      <StatPill
        stat={{
          label: "Bad cuts",
          value: totals.badCuts,
          parts: [
            { value: hitTracker.leftBadCuts, colorClass: RED },
            { value: hitTracker.rightBadCuts, colorClass: BLUE },
          ],
        }}
      />

      <StatPill stat={{ label: "Bomb hit", value: totals.bombs }} />
      <StatPill stat={{ label: "Max combo", value: maxCombo ?? "–" }} />
      <StatPill stat={{ label: "115 streak", value: hitTracker.max115Streak }} />
      <StatPill stat={{ label: "Pauses", value: winTracker.pauseCount }} />
      {Number(winTracker.pauseCount) > 0 && (
        <StatPill stat={{ label: "Pause time", value: formatTime(Number(winTracker.totalPauseDuration)) }} />
      )}
      <StatPill stat={{ label: "Accuracy", value: `${totals.accuracy.toFixed(2)}%` }} />
      <StatPill stat={{ label: "JD", value: Number(winTracker.jumpDistance).toFixed(2) }} />
    </div>
  )
}

interface StatPillPart {
  value: ReactNode
  colorClass: string
}

interface StatPillData {
  label: string
  value?: ReactNode
  parts?: StatPillPart[]
}

const StatPill = ({ stat }: { stat: StatPillData }) => (
  <Badge className="gap-1.5">
    <span className="text-muted-foreground font-medium">{stat.label}</span>
    {stat.value !== undefined && <span className="tabular-nums">{stat.value}</span>}

    {stat.parts?.map((part, index) => (
      <span
        key={index}
        className={cn(
          "flex items-center justify-center rounded-xs bg-current/10 px-1 text-xs font-semibold tabular-nums",
          part.colorClass,
        )}
      >
        {part.value}
      </span>
    ))}
  </Badge>
)
