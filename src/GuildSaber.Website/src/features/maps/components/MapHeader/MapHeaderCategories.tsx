import type { Category, RankedMap } from "@/client"
import { getCategoriesOptions } from "@/client/@tanstack/react-query.gen"
import { Badge } from "@/components/Badge"
import { Tooltip, TooltipContent, TooltipTrigger } from "@/components/ui/tooltip"
import { cn } from "@/lib/utils"
import { getCdnUrl } from "@/utils/url"
import { useQuery } from "@tanstack/react-query"
import { useState, type TouchEvent } from "react"

export const useMapCategories = (map: RankedMap) => {
  const { data: categories } = useQuery(getCategoriesOptions({ path: { guildId: String(map.guildId) } }))
  const mapCategoryIds = map.categoryIds.map(Number)
  const mapCategories = categories?.filter(
    (category) => category.id !== undefined && mapCategoryIds.includes(category.id),
  )

  return mapCategories
}

interface Props {
  map: RankedMap
  className?: string
}

export const MapHeaderCategories = ({ map, className }: Props) => {
  const mapCategories = useMapCategories(map)

  if (!mapCategories?.length) {
    return null
  }

  return (
    <div className={cn("flex gap-1", className)}>
      {mapCategories.map((category, index) => (
        <CategoryBadge key={`${category.id}-${index}`} category={category} />
      ))}
    </div>
  )
}

export const CategoryBadge = ({ category, className }: { category: Category; className?: string }) => {
  const [hasLogo, setHasLogo] = useState(true)
  const [open, setOpen] = useState(false)

  const handleTouchEnd = (e: TouchEvent) => {
    e.preventDefault()
    setOpen((v) => !v)
  }

  return (
    <Tooltip open={open} onOpenChange={setOpen}>
      <TooltipTrigger asChild>
        <span className={cn("flex cursor-default items-center", className)} onTouchEnd={handleTouchEnd}>
          {hasLogo ? (
            <img
              src={getCdnUrl(`categories/${category.id}/logo.png`)}
              onError={() => setHasLogo(false)}
              className="size-8 rounded-xs md:size-12"
            />
          ) : (
            <Badge>{category.info?.name}</Badge>
          )}
        </span>
      </TooltipTrigger>
      <TooltipContent>{category.info?.name}</TooltipContent>
    </Tooltip>
  )
}
