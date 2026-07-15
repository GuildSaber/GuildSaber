import { Badge } from "@/components/Badge"
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
  <div className={cn("flex flex-wrap gap-1.5 md:gap-2", className)}>
    <Badge>
      <Bpm className="size-3 md:size-4" /> {stats.bpm} BPM
    </Badge>
    <Badge>
      <Njs className="size-3 md:size-4" /> {stats.njs} NJS
    </Badge>
    <Badge>
      <Nps className="size-3 md:size-4" /> {parseFloat(stats.nps as string).toFixed(2)} NPS
    </Badge>
    <Badge>
      <Clock className="size-3 md:size-4" /> {stats.duration}
    </Badge>
  </div>
)
