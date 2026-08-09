import type { RankedMap } from "@/client"
import { Badge } from "@/components/Badge"
import { CategoryBadge, useMapCategories } from "@/features/maps/components/MapHeader/MapHeaderCategories"
import { MAP_DIFFICULTY, MAP_GAME_MODE } from "@/utils/constants"
import { Sparkles, Star } from "lucide-react"

interface Props {
  map: RankedMap
}

export const MapHeaderInfo = ({ map }: Props) => {
  const [{ song, difficulty: diffKey }] = map.versions
  const difficulty = MAP_DIFFICULTY[diffKey.difficulty]
  const gameMode = MAP_GAME_MODE[diffKey.gameMode] ?? diffKey.gameMode
  const mapCategories = useMapCategories(map)

  return (
    <div className="xs:items-start xs:text-left flex flex-col items-center gap-1 text-center">
      <h1 className="xs:justify-start flex flex-wrap items-baseline justify-center gap-x-2">
        <span className="line-clamp-2 text-base font-bold md:text-2xl">{song.info.name}</span>
        {song.info.authorName && (
          <span className="line-clamp-1 text-sm md:text-lg">
            <span className="text-muted-foreground">By</span> {song.info.authorName}
          </span>
        )}
      </h1>

      <p className="text-muted-foreground line-clamp-1 text-xs md:text-sm">
        mapped by <span className="text-foreground">{song.info.mapperName}</span>
      </p>

      <div className="xs:justify-start mt-2 flex flex-wrap items-center justify-center gap-1.5 md:gap-2">
        <Badge
          className="font-bold text-white"
          style={{ backgroundColor: difficulty.color, borderColor: difficulty.color }}
        >
          {difficulty?.long ?? diffKey.difficulty}
        </Badge>

        {diffKey.gameMode !== MAP_GAME_MODE.Standard && (
          <Badge className="text-muted-foreground font-bold">{gameMode}</Badge>
        )}

        <Badge className="border-amber-800 bg-amber-800/10 text-amber-900 dark:border-amber-400 dark:bg-amber-400/20 dark:text-amber-400">
          {map.rating.diffStar} <Star className="size-3 md:size-4" />
        </Badge>

        <Badge className="border-teal-800 bg-teal-800/10 text-teal-900 dark:border-teal-400 dark:bg-teal-400/20 dark:text-teal-400">
          {parseFloat(map.rating.accStar as string).toFixed(2)} <Sparkles className="size-3 md:size-4" />
        </Badge>

        {mapCategories?.map((category, index) => (
          <CategoryBadge key={`${category.id}-${index}`} category={category} className="hidden md:flex" />
        ))}

        {mapCategories?.length === 1 && <CategoryBadge category={mapCategories[0]} className="md:hidden" />}
      </div>

      {mapCategories && mapCategories.length > 1 && (
        <div className="mt-2 flex flex-wrap justify-center gap-1.5 md:hidden">
          {mapCategories.map((category, index) => (
            <CategoryBadge key={`${category.id}-${index}`} category={category} />
          ))}
        </div>
      )}
    </div>
  )
}
