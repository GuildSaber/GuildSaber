import { BadgeStat } from "@/components/BadgeStat"
import Bpm from "@/components/icons/Bpm"
import Njs from "@/components/icons/Njs"
import Nps from "@/components/icons/Nps"
import { cn } from "@/lib/utils"
import { Clock } from "lucide-react"

export interface MapStatsData {
  bpm: number | string
  njs: number | string
  nps: number | string
  duration: string
}

interface MapStatsProps {
  stats: MapStatsData
  className?: string
}

export const MapStats = ({ stats, className }: MapStatsProps) => (
  <div className={cn("flex flex-wrap gap-2", className)}>
    <BadgeStat icon={<Bpm className="size-4" />} label={`${stats.bpm} BPM`} />
    <BadgeStat icon={<Njs className="size-4" />} label={`${stats.njs} NJS`} />
    <BadgeStat icon={<Nps className="size-4" />} label={`${parseFloat(stats.nps as string).toFixed(2)} NPS`} />
    <BadgeStat icon={<Clock className="size-4" />} label={stats.duration} />
  </div>
)
