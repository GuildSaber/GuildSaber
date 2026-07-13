import type { ScoreStatistics } from "@/client"
import { ChartContainer, ChartTooltip, ChartTooltipContent, type ChartConfig } from "@/components/ui/chart"
import { formatTime } from "@/utils/time"
import { useId, useMemo } from "react"
import { Area, AreaChart, CartesianGrid, XAxis, YAxis } from "recharts"

interface Props {
  data: ScoreStatistics
}

const chartConfig = {
  accuracy: {
    label: "Accuracy",
    color: "var(--chart-1)",
  },
} satisfies ChartConfig

interface ChartPoint {
  time: number
  accuracy: number
}

const formatTimeTick = (value: number) => formatTime(Math.round(value))
const formatPercentTick = (value: number) => `${value.toFixed(0)}%`

const formatTooltipLabel = (_: unknown, payload: ReadonlyArray<{ payload?: ChartPoint }>) => {
  const time = payload?.[0]?.payload?.time

  return typeof time === "number" ? formatTime(Math.round(time)) : null
}

const renderTooltipValue = (value: unknown, _name: unknown, item: { color?: string }) => (
  <div className="flex w-full items-center justify-between gap-3">
    <span className="text-muted-foreground flex items-center gap-1.5">
      <span className="size-2 shrink-0 rounded-full" style={{ backgroundColor: item.color }} />
      Accuracy
    </span>
    <span className="text-foreground font-mono font-medium tabular-nums">{Number(value).toFixed(1)}%</span>
  </div>
)

export const MapScoreStatsGraphScore = ({ data: statistics }: Props) => {
  const {
    winTracker,
    scoreGraphTracker: { graph },
  } = statistics
  const gradientId = useId()
  const durationSeconds = Number(winTracker.endTime)

  const data = useMemo<ChartPoint[]>(() => {
    const points = graph.map((value, index) => ({
      time: graph.length > 1 ? (index / (graph.length - 1)) * durationSeconds : 0,
      accuracy: Number(value) * 100,
    }))

    if (points.length > 0) {
      points[0] = { ...points[0], accuracy: 100 }
    }

    return points
  }, [graph, durationSeconds])

  const yTicks = useMemo(() => {
    if (data.length === 0) {
      return [0, 100]
    }

    const min = Math.floor(Math.min(...data.map((point) => point.accuracy)))

    return Array.from({ length: 100 - min + 1 }, (_, index) => min + index)
  }, [data])

  return (
    <ChartContainer config={chartConfig} className="aspect-auto h-full w-full">
      <AreaChart data={data} margin={{ left: 0, right: 8, top: 8, bottom: 0 }}>
        <defs>
          <linearGradient id={gradientId} x1="0" y1="0" x2="0" y2="1">
            <stop offset="5%" stopColor="var(--color-accuracy)" stopOpacity={0.4} />
            <stop offset="95%" stopColor="var(--color-accuracy)" stopOpacity={0} />
          </linearGradient>
        </defs>

        <CartesianGrid vertical={false} />

        <XAxis
          dataKey="time"
          type="number"
          domain={[0, durationSeconds]}
          tickFormatter={formatTimeTick}
          tickLine={false}
          axisLine={false}
          fontSize={10}
        />

        <YAxis
          domain={[yTicks[0] - 1, 100]}
          ticks={yTicks}
          tickFormatter={formatPercentTick}
          tickLine={false}
          axisLine={false}
          fontSize={10}
          width={36}
        />

        <ChartTooltip
          cursor
          content={
            <ChartTooltipContent
              formatter={renderTooltipValue}
              labelFormatter={formatTooltipLabel}
              labelClassName="text-sm font-semibold"
            />
          }
        />

        <Area
          dataKey="accuracy"
          type="monotone"
          stroke="var(--color-accuracy)"
          strokeWidth={1.5}
          fill={`url(#${gradientId})`}
          dot={false}
          activeDot={{ r: 3 }}
          isAnimationActive={false}
        />
      </AreaChart>
    </ChartContainer>
  )
}
