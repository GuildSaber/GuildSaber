import type { RankedMapWithScores, RankedScore } from "@/client"
import { Badge } from "@/components/Badge"
import BeatSaver from "@/components/icons/BeatSaver"
import Twitch from "@/components/icons/Twitch"
import Image from "@/components/Image"
import { Button } from "@/components/ui/button"
import { Card, CardContent } from "@/components/ui/card"
import GuildMapRowCategories from "@/features/guilds/components/GuildMaps/GuildMapRow/GuildMapRowCategories"
import { useArcViewerStore } from "@/features/maps/stores/arcViewerStore"
import { formatDate } from "@/features/maps/utils"
import { cn } from "@/lib/utils"
import { getMapCover } from "@/utils/beatsaver"
import { MAP_DIFFICULTY } from "@/utils/constants"
import { getAccuracy } from "@/utils/score"
import { DownloadCloud, Play, Sparkles, Star } from "lucide-react"
import { Link } from "react-router"
import { useCopyToClipboard } from "usehooks-ts"

const STATUS_COLORS: Record<RankedScore["type"], string> = {
  Valid:
    "border-green-800 bg-green-800/10 text-green-900 dark:border-green-400 dark:bg-green-400/20 dark:text-green-400",
  Accepted: "border-blue-800 bg-blue-800/10 text-blue-900 dark:border-blue-400 dark:bg-blue-400/20 dark:text-blue-400",
  Pending:
    "border-amber-800 bg-amber-800/10 text-amber-900 dark:border-amber-400 dark:bg-amber-400/20 dark:text-amber-400",
  Refused:
    "border-red-800 bg-red-800/10 bg-[image:repeating-linear-gradient(45deg,rgba(153,27,27,0.1)_0px,rgba(153,27,27,0.1)_6px,transparent_6px,transparent_12px)] text-red-900 dark:border-red-400 dark:bg-red-400/10 dark:bg-[image:repeating-linear-gradient(45deg,rgba(248,113,113,0.1)_0px,rgba(248,113,113,0.1)_6px,transparent_6px,transparent_12px)] dark:text-red-400",
  Invalid: "border-red-800 bg-red-800/10 text-red-900 dark:border-red-400 dark:bg-red-400/20 dark:text-red-400",
}

interface Props {
  score: RankedMapWithScores
}

const GuildMapRow = ({ score: { rankedMap, rankedScores } }: Props) => {
  const [{ song, difficulty: diffKey }] = rankedMap.versions
  const difficulty = MAP_DIFFICULTY[diffKey.difficulty]

  const [rankedScore] = rankedScores

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
    <div className="flex flex-col md:flex-row md:items-stretch">
      <Card
        className={cn(
          "flex flex-1 justify-center py-3",
          rankedScore && "rounded-none rounded-t-xl md:rounded-l-xl md:rounded-tr-none",
        )}
      >
        <CardContent className="flex flex-col px-3 md:flex-row md:items-center">
          <div className="grid flex-1 grid-cols-[auto_minmax(0,1fr)_auto] items-center gap-4 md:grid-cols-[auto_minmax(0,1fr)_auto_auto]">
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
              <Link to={`/maps/${rankedMap.id}`} className="group">
                <p className="line-clamp-2 min-w-0 overflow-hidden break-all text-blue-400 group-hover:underline">
                  {song.info.name}
                </p>
              </Link>
              <p className="line-clamp-1 text-sm">
                {song.info.authorName} <span className="text-muted-foreground text-xs">{song.info.mapperName}</span>
              </p>

              <GuildMapRowCategories map={rankedMap} />
            </div>

            <div className="flex flex-col items-end justify-end gap-1">
              <Badge className="h-7 border-amber-800 bg-amber-800/10 px-2 py-1 text-amber-900 dark:border-amber-400 dark:bg-amber-400/20 dark:text-amber-400">
                {rankedMap.rating.diffStar} <Star className="size-4" />
              </Badge>
              <Badge className="h-7 border-teal-800 bg-teal-800/10 px-2 py-1 text-teal-900 dark:border-teal-400 dark:bg-teal-400/20 dark:text-teal-400">
                {parseFloat(rankedMap.rating.accStar as string).toFixed(2)} <Sparkles className="size-4" />
              </Badge>
            </div>
          </div>

          <div className="mt-2 grid grid-cols-4 items-end gap-1 md:mt-0 md:grid-cols-2">
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
        </CardContent>
      </Card>

      {rankedScore && (
        <div
          className={cn(
            "flex shrink-0 flex-row items-center justify-between gap-1 rounded-b-xl border px-4 py-2 md:w-24 md:flex-col md:justify-center md:rounded-r-xl md:rounded-bl-none md:px-0",
            STATUS_COLORS[rankedScore.type],
          )}
        >
          <span className="text-xs opacity-80">{rankedScore.type}</span>
          <span className="text-xs font-semibold md:text-base">
            {`${getAccuracy(rankedMap, rankedScore).toFixed(2)}%`}
          </span>

          <span className="text-xs opacity-80">{formatDate(rankedScore.score.setAt, "long")}</span>
        </div>
      )}
    </div>
  )
}

export default GuildMapRow
