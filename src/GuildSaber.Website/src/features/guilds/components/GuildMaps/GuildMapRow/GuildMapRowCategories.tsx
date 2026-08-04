import type { Category, RankedMap } from "@/client"
import { Badge } from "@/components/Badge"
import { Tooltip, TooltipContent, TooltipTrigger } from "@/components/ui/tooltip"
import { useMapCategories } from "@/features/maps/components/MapHeader/MapHeaderCategories"
import { getCdnUrl } from "@/utils/url"
import { useState, type TouchEvent } from "react"

interface Props {
  map: RankedMap
}

const GuildMapRowCategories = ({ map }: Props) => {
  const mapCategories = useMapCategories(map)

  if (!mapCategories?.length) {
    return null
  }

  return (
    <div className="mt-1 flex gap-1">
      {mapCategories.map((category, index) => (
        <GuildMapRowCategoryBadge key={`${category.id}-${index}`} category={category} />
      ))}
    </div>
  )
}

const GuildMapRowCategoryBadge = ({ category }: { category: Category }) => {
  const [hasLogo, setHasLogo] = useState(true)
  const [open, setOpen] = useState(false)

  const handleTouchEnd = (e: TouchEvent) => {
    e.preventDefault()
    setOpen((v) => !v)
  }

  return (
    <Tooltip open={open} onOpenChange={setOpen}>
      <TooltipTrigger asChild>
        <span className="flex cursor-default items-center" onTouchEnd={handleTouchEnd}>
          {hasLogo ? (
            <img
              src={getCdnUrl(`categories/${category.id}/logo.png`)}
              onError={() => setHasLogo(false)}
              className="size-7 rounded-xs"
            />
          ) : (
            <Badge className="px-1 py-0 text-[10px]">{category.info?.name}</Badge>
          )}
        </span>
      </TooltipTrigger>
      <TooltipContent>{category.info?.name}</TooltipContent>
    </Tooltip>
  )
}

export default GuildMapRowCategories
