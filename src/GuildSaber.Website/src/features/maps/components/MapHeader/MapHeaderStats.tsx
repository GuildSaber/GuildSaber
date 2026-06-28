import Bpm from "@/components/icons/Bpm"
import Njs from "@/components/icons/Njs"
import Nps from "@/components/icons/Nps"
import { cn } from "@/lib/utils"
import { Clock } from "lucide-react"

export const Stat = ({ icon, label }: { icon: React.ReactNode; label: string }) => (
  <div className="text-muted-foreground border-input flex items-center gap-1 rounded border px-1.5 py-0.5 text-xs md:px-2 md:py-1 md:text-sm">
    {icon}
    <span>{label}</span>
  </div>
)

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
    <Stat icon={<Bpm className="size-4" />} label={`${stats.bpm} BPM`} />
    <Stat icon={<Njs className="size-4" />} label={`${stats.njs} NJS`} />
    <Stat icon={<Nps className="size-4" />} label={`${parseFloat(stats.nps as string).toFixed(2)} NPS`} />
    <Stat icon={<Clock className="size-4" />} label={stats.duration} />
  </div>
)
