import BeatLeader from "@/components/icons/BeatLeaderColor"
import ChroViewer from "@/components/icons/ChroViewer"
import type { ReplayViewer } from "@/features/maps/stores/replayStore"
import { cn } from "@/lib/utils"

interface Props {
  viewer: ReplayViewer
  className?: string
}

const ReplayViewerIcon = ({ viewer, className }: Props) =>
  viewer === "BeatLeader" ? (
    <BeatLeader className={cn("size-4", className)} />
  ) : (
    <ChroViewer className={cn("size-4", className)} />
  )

export default ReplayViewerIcon
