import type { RankedMap } from "@/client"
import BeatSaver from "@/components/icons/BeatSaver"
import Twitch from "@/components/icons/Twitch"
import { Button } from "@/components/ui/button"
import { useArcViewerStore } from "@/features/maps/stores/arcViewerStore"
import { cn } from "@/lib/utils"
import { DownloadCloud, Play } from "lucide-react"
import { useCopyToClipboard } from "usehooks-ts"

interface Props {
  map: RankedMap
  className?: string
}

export const MapHeaderActions = ({ map, className }: Props) => {
  const [{ song, difficulty: diffKey }] = map.versions
  const { openArcViewer } = useArcViewerStore()
  const [, copy] = useCopyToClipboard()

  const handleBeatSaverLink = () => {
    window.open(`https://beatsaver.com/maps/${song.key}`, "_blank", "noopener,noreferrer")
  }

  const handleArcViewer = () => {
    openArcViewer(song.key, diffKey.difficulty, diffKey.gameMode)
  }

  const handleTwitchReplay = () => {
    copy(`!bsr ${song.key}`)
  }

  const handleOneClickInstall = () => {
    window.location.href = `beatsaver://${song.key}`
  }

  return (
    <div className={cn("flex gap-1", className)}>
      <Button
        title="BeatSaver"
        onClick={handleBeatSaverLink}
        size="xs"
        variant="outline"
        className="hover:border-pink-500!"
      >
        <BeatSaver className="size-4" />
      </Button>
      <Button
        title="Arc Viewer"
        onClick={handleArcViewer}
        size="xs"
        variant="outline"
        className="hover:border-red-700!"
      >
        <Play className="size-4" />
      </Button>
      <Button
        title="Copy !bsr for Twitch"
        onClick={handleTwitchReplay}
        size="xs"
        variant="outline"
        className="hover:border-purple-700!"
      >
        <Twitch className="size-4" />
      </Button>
      <Button
        title="One-click install"
        onClick={handleOneClickInstall}
        size="xs"
        variant="outline"
        className="hover:border-primary!"
      >
        <DownloadCloud className="size-4" />
      </Button>
    </div>
  )
}
