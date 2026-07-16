import type { GetRankedMapResponse } from "@/client"
import { Badge } from "@/components/Badge"
import BeatSaver from "@/components/icons/BeatSaver"
import Twitch from "@/components/icons/Twitch"
import Image from "@/components/Image"
import { Button } from "@/components/ui/button"
import { useArcViewerStore } from "@/features/maps/stores/arcViewerStore"
import { getMapCover } from "@/utils/beatsaver"
import { MAP_DIFFICULTY } from "@/utils/constants"
import { DownloadCloud, Play, Sparkles, Star } from "lucide-react"
import { Link } from "react-router"
import { useCopyToClipboard } from "usehooks-ts"

interface Props {
  map: GetRankedMapResponse
}

const GuildMapRow = ({ map }: Props) => {
  const [{ song, difficulty: diffKey }] = map.versions
  const difficulty = MAP_DIFFICULTY[diffKey.difficulty]

  const { openArcViewer } = useArcViewerStore()
  const [, copy] = useCopyToClipboard()

  const handleBeatSaverLink = () => {
    window.open(`https://beatsaver.com/maps/${song.key}`, "_blank", "noopener,noreferrer")
  }

  const handleTwitchReplay = () => {
    copy(`!bsr ${song.key}`)
  }

  const handleOneClickInstall = () => {
    window.location.href = `beatsaver://${song.key}`
  }

  const handleArcViewer = () => {
    openArcViewer(song.key, diffKey.difficulty, diffKey.gameMode)
  }

  return (
    <div className="flex flex-col md:flex-row md:items-center">
      <div className="grid flex-1 grid-cols-[auto_minmax(0,1fr)_auto] items-center gap-4 py-2 sm:grid-cols-[auto_minmax(0,1fr)_auto_auto]">
        <div className="relative size-16 md:size-18">
          <Image src={getMapCover(song.hash)} className="size-16 rounded md:size-18" />
          <p
            className="absolute -right-2 -bottom-1 rounded px-1 py-0.5 text-xs text-white"
            style={{ backgroundColor: difficulty.color }}
          >
            {difficulty.short}
          </p>
        </div>

        <div>
          <Link to={`/maps/${map.id}`} className="group">
            <p className="line-clamp-2 min-w-0 overflow-hidden break-all text-blue-400 group-hover:underline">
              {song.info.name}
            </p>
          </Link>
          <p className="text-sm">
            {song.info.authorName} <span className="text-muted-foreground text-xs">{song.info.mapperName}</span>
          </p>
        </div>

        <div className="flex flex-col items-end justify-end gap-1">
          <Badge className="h-7 border-amber-800 bg-amber-800/10 px-2 py-1 text-amber-900 dark:border-amber-400 dark:bg-amber-400/20 dark:text-amber-400">
            {map.rating.diffStar} <Star className="size-4" />
          </Badge>
          <Badge className="h-7 border-teal-800 bg-teal-800/10 px-2 py-1 text-teal-900 dark:border-teal-400 dark:bg-teal-400/20 dark:text-teal-400">
            {parseFloat(map.rating.accStar as string).toFixed(2)} <Sparkles className="size-4" />
          </Badge>
        </div>
      </div>

      <div className="mb-2 grid grid-cols-4 items-end gap-1 sm:grid-cols-2 md:mb-0">
        <Button
          title="Beatsaber link"
          onClick={handleBeatSaverLink}
          size="xs"
          variant="outline"
          className="hover:border-pink-500!"
        >
          <BeatSaver className="size-4" />
        </Button>
        <Button
          title="View map in Arc Viewer"
          onClick={handleArcViewer}
          size="xs"
          variant="outline"
          className="hover:border-red-700!"
        >
          <Play className="size-4" />
        </Button>
        <Button
          title="Copy bsr code for Twitch request"
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
    </div>
  )
}

export default GuildMapRow
