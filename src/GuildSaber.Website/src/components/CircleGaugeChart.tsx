import { cn } from "@/lib/utils"
import { useState } from "react"
import { Pie, PieChart, ResponsiveContainer } from "recharts"

const GAP_DEG = 6
const START_ANGLE = -90
const TRACK_OPACITY = 0.25
const TOOLTIP_OFFSET = 4

interface PolarPoint {
  cx: number
  cy: number
  radius: number
  angleDeg: number
}

const polarToCartesian = ({ cx, cy, radius, angleDeg }: PolarPoint) => {
  const rad = (angleDeg * Math.PI) / 180

  return { x: cx + Math.cos(-rad) * radius, y: cy + Math.sin(-rad) * radius }
}

interface CornerRadiusParams {
  sweepDeg: number
  midRadius: number
  ringWidth: number
  maxCorner: number
}

const safeCornerRadius = ({ sweepDeg, midRadius, ringWidth, maxCorner }: CornerRadiusParams) => {
  const arcLength = (Math.abs(sweepDeg) * Math.PI * midRadius) / 180

  return Math.max(0, Math.min(maxCorner, arcLength / 2, ringWidth / 2))
}

interface SlotAngle {
  startAngle: number
  endAngle: number
}

const getSlotAngles = (sizes: number[], total: number): SlotAngle[] => {
  const degPerUnit = 360 / total
  let cumulative = 0

  return sizes.map((size) => {
    const startDeg = cumulative * degPerUnit
    const endDeg = (cumulative + size) * degPerUnit
    cumulative += size

    const nominalStart = START_ANGLE + startDeg
    const nominalEnd = START_ANGLE + endDeg

    return {
      startAngle: nominalStart + GAP_DEG / 2,
      endAngle: nominalEnd - GAP_DEG / 2,
    }
  })
}

interface GaugeDatum {
  value: number
  max: number
  name?: string
}

const SINGLE_SLICE = [{ value: 1 }]

interface GaugeChartProps {
  color?: string
  data?: GaugeDatum[]
  mirror?: boolean
  size?: number
  ringWidth?: number
  className?: string
  valueClassName?: string
  centerValue?: number
}

const DEFAULT_DATA: GaugeDatum[] = [
  { value: 13, max: 15 },
  { value: 20, max: 70 },
  { value: 15, max: 30 },
]

export const GaugeChart = ({
  color = "#FF6467",
  data = DEFAULT_DATA,
  mirror = false,
  size = 300,
  className,
  valueClassName,
  centerValue,
  ringWidth,
}: GaugeChartProps) => {
  const [activeIndex, setActiveIndex] = useState<number | null>(null)

  const resolvedOuter = size * 0.45
  const resolvedInner = resolvedOuter - (ringWidth ?? size * 0.1167)
  const midRadius = (resolvedInner + resolvedOuter) / 2
  const resolvedRingWidth = resolvedOuter - resolvedInner
  const maxCorner = 8 * (size / 300)
  const fontSize = 36 * (size / 300)

  const sizes = data.map((d) => d.max)
  const fills = data.map((d) => d.value)
  const total = sizes.reduce((a, b) => a + b, 0)
  const angles = getSlotAngles(sizes, total)
  const totalFill = fills.reduce((a, b) => a + b, 0)

  const cx = size / 2
  const cy = size / 2
  const tooltipAnchor =
    activeIndex === null
      ? null
      : polarToCartesian({
          cx,
          cy,
          radius: resolvedOuter + TOOLTIP_OFFSET,
          angleDeg: (angles[activeIndex].startAngle + angles[activeIndex].endAngle) / 2,
        })
  const tooltipTransform = tooltipAnchor
    ? `translate(${tooltipAnchor.x}px, ${tooltipAnchor.y}px) translate(${tooltipAnchor.x < cx ? "-100%" : "0%"}, ${tooltipAnchor.y < cy ? "-100%" : "0%"})`
    : undefined

  const handleMouseEnter = (index: number) => () => setActiveIndex(index)
  const handleMouseLeave = () => setActiveIndex(null)
  const handleClick = (index: number) => () => setActiveIndex((current) => (current === index ? null : index))

  return (
    <div
      className={cn("relative", className)}
      style={{ width: size, height: size, transform: mirror ? "scaleX(-1)" : "none" }}
    >
      <ResponsiveContainer width="100%" height="100%">
        <PieChart margin={{ top: 0, right: 0, bottom: 0, left: 0 }}>
          {sizes.flatMap((slotSize, i) => {
            const fill = fills[i]
            const fraction = slotSize === 0 ? 0 : fill / slotSize
            const { startAngle, endAngle } = angles[i]
            const sweep = endAngle - startAngle
            const fillEndAngle = startAngle + sweep * fraction
            const trackCorner = safeCornerRadius({
              sweepDeg: sweep,
              midRadius,
              ringWidth: resolvedRingWidth,
              maxCorner,
            })
            const fillCorner = safeCornerRadius({
              sweepDeg: fillEndAngle - startAngle,
              midRadius,
              ringWidth: resolvedRingWidth,
              maxCorner,
            })

            const pies = [
              <Pie
                key={`track-${i}`}
                data={SINGLE_SLICE}
                dataKey="value"
                startAngle={startAngle}
                endAngle={endAngle}
                innerRadius={resolvedInner}
                outerRadius={resolvedOuter}
                cornerRadius={trackCorner}
                fill={color}
                fillOpacity={TRACK_OPACITY}
                stroke="none"
                isAnimationActive={false}
                onMouseEnter={handleMouseEnter(i)}
                onMouseLeave={handleMouseLeave}
                onClick={handleClick(i)}
              />,
            ]

            if (fraction > 0) {
              pies.push(
                <Pie
                  key={`fill-${i}`}
                  data={SINGLE_SLICE}
                  dataKey="value"
                  startAngle={startAngle}
                  endAngle={fillEndAngle}
                  innerRadius={resolvedInner}
                  outerRadius={resolvedOuter}
                  cornerRadius={fillCorner}
                  fill={color}
                  stroke="none"
                  isAnimationActive={false}
                  onMouseEnter={handleMouseEnter(i)}
                  onMouseLeave={handleMouseLeave}
                  onClick={handleClick(i)}
                />,
              )
            }

            return pies
          })}
        </PieChart>
      </ResponsiveContainer>

      <div
        className="pointer-events-none absolute inset-0 flex items-center justify-center"
        style={{ transform: mirror ? "scaleX(-1)" : "none" }}
      >
        <span
          className={cn("text-foreground font-sans", valueClassName ?? "font-bold")}
          style={valueClassName ? undefined : { fontSize }}
        >
          {Number((centerValue ?? totalFill).toFixed(2))}
        </span>
      </div>

      {activeIndex !== null && (
        <div className="pointer-events-none absolute top-0 left-0 z-50" style={{ transform: tooltipTransform }}>
          <div
            className="bg-foreground text-background rounded-md px-3 py-1.5 text-xs"
            style={{ transform: mirror ? "scaleX(-1)" : "none" }}
          >
            <div className="font-semibold">{data[activeIndex].name ?? `Gauge ${activeIndex + 1}`}</div>
            <div className="opacity-70">{`${Number(fills[activeIndex].toFixed(2))} / ${sizes[activeIndex]}`}</div>
          </div>
        </div>
      )}
    </div>
  )
}
