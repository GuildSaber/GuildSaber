import type { RankedMap } from "@/client"
import { MAP_DIFFICULTY, MAP_GAME_MODE } from "@/utils/constants"
import { Sparkles, Star } from "lucide-react"

interface Props {
  map: RankedMap
}

export const MapHeaderInfo = ({ map }: Props) => {
  const [{ song, difficulty: diffKey }] = map.versions
  const difficulty = MAP_DIFFICULTY[diffKey.difficulty]
  const gameMode = MAP_GAME_MODE[diffKey.gameMode] ?? diffKey.gameMode

  return (
    <div className="flex flex-col gap-1">
      <h1 className="line-clamp-2 text-base font-bold md:text-2xl">{song.info.name}</h1>
      {song.info.subName && (
        <p className="text-muted-foreground line-clamp-1 text-xs md:text-sm">{song.info.subName}</p>
      )}

      {song.info.authorName && (
        <p className="line-clamp-1 text-sm md:text-lg">
          <span className="text-muted-foreground">By</span> {song.info.authorName}
        </p>
      )}

      <p className="text-muted-foreground line-clamp-1 text-xs md:text-sm">
        mapped by <span className="text-foreground">{song.info.mapperName}</span>
      </p>

      <div className="mt-2 flex flex-wrap items-center gap-1.5 md:gap-2">
        <div
          className="flex items-center gap-1 rounded border px-1.5 py-0.5 text-xs font-bold text-white md:px-2 md:py-1 md:text-sm"
          style={{ backgroundColor: difficulty.color, borderColor: difficulty.color }}
        >
          {difficulty?.long ?? diffKey.difficulty}
        </div>

        {diffKey.gameMode !== MAP_GAME_MODE.Standard && (
          <div className="text-muted-foreground flex h-6 items-center gap-1 rounded border px-1.5 text-xs font-bold md:h-7 md:px-2 md:text-sm">
            {gameMode}
          </div>
        )}

        <div className="flex h-6 items-center gap-1 rounded border border-amber-800 bg-amber-800/10 px-1.5 text-xs text-amber-900 md:h-7 md:px-2 md:text-sm dark:border-amber-400 dark:bg-amber-400/20 dark:text-amber-400">
          {map.rating.diffStar} <Star className="size-3 md:size-4" />
        </div>

        <div className="flex h-6 items-center gap-1 rounded border border-teal-800 bg-teal-800/10 px-1.5 text-xs text-teal-900 md:h-7 md:px-2 md:text-sm dark:border-teal-400 dark:bg-teal-400/20 dark:text-teal-400">
          {parseFloat(map.rating.accStar as string).toFixed(2)} <Sparkles className="size-3 md:size-4" />
        </div>
      </div>
    </div>
  )
}
