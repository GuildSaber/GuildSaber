import type { AccuracyTracker } from "@/client"
import { Badge } from "@/components/Badge"
import { cn } from "@/lib/utils"

const RING_CIRCUMFERENCE = 100
const RING_RADIUS = 15.9155
const RING_MAX = 115

const RED = "text-red-600 dark:text-red-400"
const BLUE = "text-blue-600 dark:text-blue-400"

const HANDS = {
  LEFT: "left",
  RIGHT: "right",
} as const

type Hand = (typeof HANDS)[keyof typeof HANDS]

interface Props {
  accuracyTracker: AccuracyTracker
}

const HAND_KEYS = {
  [HANDS.LEFT]: {
    acc: "accLeft",
    grid: "leftAverageCutGraphGrid",
    timeDependence: "leftTimeDependence",
    preSwing: "leftPreSwing",
    postSwing: "leftPostSwing",
  },
  [HANDS.RIGHT]: {
    acc: "accRight",
    grid: "rightAverageCutGraphGrid",
    timeDependence: "rightTimeDependence",
    preSwing: "rightPreSwing",
    postSwing: "rightPostSwing",
  },
} as const

const toRingData = (tracker: AccuracyTracker, hand: Hand) => {
  const keys = HAND_KEYS[hand]

  return {
    hand,
    colorClass: hand === HANDS.LEFT ? RED : BLUE,
    value: Number(tracker[keys.acc]),
    grid: tracker[keys.grid].map(Number) as [number, number, number],
    timeDependence: Number(tracker[keys.timeDependence]),
    preSwing: Number(tracker[keys.preSwing]),
    postSwing: Number(tracker[keys.postSwing]),
  }
}

export const MapScoreStatsHandStats = ({ accuracyTracker }: Props) => (
  <div className="flex flex-wrap items-start justify-center gap-6">
    <AccuracyRing data={toRingData(accuracyTracker, HANDS.LEFT)} />
    <AccuracyRing data={toRingData(accuracyTracker, HANDS.RIGHT)} />
  </div>
)

interface AccuracyRingData {
  hand: Hand
  colorClass: string
  value: number
  grid: [number, number, number]
  timeDependence: number
  preSwing: number
  postSwing: number
}

const toSwingPills = (data: AccuracyRingData) => [
  { label: "TD", value: data.timeDependence.toFixed(3) },
  { label: "PRE", value: `${(data.preSwing * 100).toFixed(2)}%` },
  { label: "POST", value: `${(data.postSwing * 100).toFixed(2)}%` },
]

const AccuracyRing = ({ data }: { data: AccuracyRingData }) => {
  const { hand, colorClass, value, grid } = data
  const isLeft = hand === HANDS.LEFT
  const pct = (Math.min(Math.max(value, 0), RING_MAX) / RING_MAX) * 100
  const pills = toSwingPills(data)

  return (
    <div className="flex flex-col items-center gap-1.5">
      <span className="text-muted-foreground flex h-4 items-center text-[11px] font-medium tracking-wide uppercase">
        {isLeft ? "Left hand" : "Right hand"}
      </span>

      <div className={cn("flex items-center gap-3", !isLeft && "flex-row-reverse")}>
        <div className="hidden flex-col gap-1 sm:flex">
          {pills.map((pill) => (
            <Badge
              key={pill.label}
              className={cn(
                "justify-between gap-1.5 border-current/30 bg-current/10 text-xs font-medium tabular-nums",
                colorClass,
              )}
            >
              <span className="opacity-70">{pill.label}</span>
              {pill.value}
            </Badge>
          ))}
        </div>

        <div className={cn("flex items-center gap-2", !isLeft && "flex-row-reverse")}>
          <div className={cn("flex flex-col items-center gap-0.5 text-sm tabular-nums", colorClass)}>
            <span>{grid[0].toFixed(2)}</span>
            <span>{grid[1].toFixed(2)}</span>
            <span>{grid[2].toFixed(2)}</span>
          </div>

          <div className={cn("relative flex size-20 shrink-0 items-center justify-center md:size-22", colorClass)}>
            <svg viewBox="0 0 36 36" className={cn("size-20 -rotate-90 md:size-22", !isLeft && "-scale-y-100")}>
              <circle cx="18" cy="18" r={RING_RADIUS} fill="none" className="stroke-muted" strokeWidth="2.5" />
              <circle
                cx="18"
                cy="18"
                r={RING_RADIUS}
                fill="none"
                stroke="currentColor"
                strokeWidth="2.5"
                strokeLinecap="round"
                strokeDasharray={`${pct} ${RING_CIRCUMFERENCE - pct}`}
              />
            </svg>
            <span className="text-foreground absolute text-base font-semibold tabular-nums sm:text-lg">
              {value.toFixed(2)}
            </span>
          </div>
        </div>
      </div>

      <div className="flex w-full flex-col gap-1 sm:hidden">
        {pills.map((pill) => (
          <Badge
            key={pill.label}
            className={cn(
              "w-full justify-between gap-1.5 border-current/30 bg-current/10 text-xs font-medium tabular-nums",
              colorClass,
            )}
          >
            <span className="opacity-70">{pill.label}</span>
            {pill.value}
          </Badge>
        ))}
      </div>
    </div>
  )
}
