import type { AccuracyTracker } from "@/client"
import { Badge } from "@/components/Badge"
import { GaugeChart } from "@/components/CircleGaugeChart"
import { cn } from "@/lib/utils"
import { useMediaQuery } from "usehooks-ts"

const GRID_MAX = [70, 15, 30] as const
const GRID_LABELS = ["Before-cut", "Accuracy", "After-cut"] as const

const GAUGE_SIZE = 80
const GAUGE_SIZE_MD = 88

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

const toGaugeData = (grid: [number, number, number]) =>
  grid.map((value, i) => ({ value, max: GRID_MAX[i], name: GRID_LABELS[i] }))

const AccuracyRing = ({ data }: { data: AccuracyRingData }) => {
  const { hand, colorClass, value, grid } = data
  const isLeft = hand === HANDS.LEFT
  const isMdUp = useMediaQuery("(min-width: 768px)")
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

        <div className={cn("flex size-20 shrink-0 items-center justify-center md:size-22", colorClass)}>
          <GaugeChart
            color="currentColor"
            data={toGaugeData(grid)}
            mirror={!isLeft}
            size={isMdUp ? GAUGE_SIZE_MD : GAUGE_SIZE}
            ringWidth={8}
            centerValue={value}
            valueClassName="text-base font-semibold tabular-nums"
          />
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
