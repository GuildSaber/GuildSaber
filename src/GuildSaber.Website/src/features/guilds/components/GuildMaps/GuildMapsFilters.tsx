import type { Category, EOrder, ERankedMapSorter } from "@/client"
import { Button } from "@/components/ui/button"
import { Checkbox } from "@/components/ui/checkbox"
import { Field, FieldDescription, FieldGroup, FieldLabel, FieldSeparator } from "@/components/ui/field"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select"
import { Slider } from "@/components/ui/slider"
import { useGuildMapFilters } from "@/features/guilds/hooks/useGuildMapFilters"
import { cn } from "@/lib/utils"
import { MAP_SORT_BY, MAX_BPM, MAX_STARS, MIN_BPM, MIN_STARS, ORDER_BY } from "@/utils/constants"
import { useState, type ChangeEvent } from "react"
import { useDebounceCallback } from "usehooks-ts"

type Props = {
  categories?: Category[]
}

const GuildMapsFilters = ({ categories }: Props) => {
  const [filters, setFilters] = useGuildMapFilters()

  const [localStars, setLocalStars] = useState(filters.stars)
  const [localBpm, setLocalBpm] = useState(filters.bpm)

  const updateFilters = (newFilters: Partial<typeof filters>) => {
    setFilters({ ...newFilters, page: 1 })
  }

  const handleResetFilters = () => {
    setLocalStars([MIN_STARS, MAX_STARS])
    setLocalBpm([MIN_BPM, MAX_BPM])
    setFilters(null)
  }

  const handleSearchChange = (e: ChangeEvent<HTMLInputElement>) => {
    updateFilters({ search: e.target.value })
  }

  const debouncedSearch = useDebounceCallback(handleSearchChange, 300)

  const handleOrderChange = (v: EOrder) => {
    updateFilters({ order: v })
  }

  const handleSortChange = (v: ERankedMapSorter) => {
    updateFilters({ sort: v })
  }

  const handleStarsChange = (v: number[]) => {
    updateFilters({ stars: v })
  }

  const handleBpmChange = (v: number[]) => {
    updateFilters({ bpm: v })
  }

  const handleCategoriesSelect = (categoryId: number) => () => {
    const { categories } = filters

    if (categories.includes(categoryId)) {
      updateFilters({ categories: categories.filter((id) => id !== categoryId) })

      return
    }

    updateFilters({ categories: [...categories, categoryId] })
  }

  const handleMatchAnyCategoryChange = (v: boolean) => {
    updateFilters({ matchAnyCategory: v })
  }

  return (
    <FieldGroup className="gap-4">
      <Field>
        <FieldLabel>Search</FieldLabel>
        <Input placeholder="Search..." defaultValue={filters.search} onChange={debouncedSearch} />
      </Field>

      <FieldSeparator />

      <Field>
        <FieldLabel>Order</FieldLabel>
        <Select value={filters.order} onValueChange={handleOrderChange}>
          <SelectTrigger id="checkout-exp-month-ts6">
            <SelectValue placeholder="Order by" />
          </SelectTrigger>
          <SelectContent>
            {Object.entries(ORDER_BY).map(([value, label]) => (
              <SelectItem key={value} value={value}>
                {label}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
      </Field>

      <Field>
        <FieldLabel>Sort</FieldLabel>
        <Select value={filters.sort} onValueChange={handleSortChange}>
          <SelectTrigger id="checkout-exp-month-ts6">
            <SelectValue placeholder="Sort by" />
          </SelectTrigger>
          <SelectContent>
            {Object.entries(MAP_SORT_BY).map(([value, label]) => (
              <SelectItem key={value} value={value}>
                {label}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
      </Field>

      <FieldSeparator />

      <Field>
        <FieldLabel>Stars</FieldLabel>
        <FieldDescription>
          Only show maps with Stars between <span className="font-semibold text-amber-400">{localStars[0]}</span> and{" "}
          <span className="font-semibold text-amber-400">{localStars[1]}</span>
        </FieldDescription>
        <Slider
          className="**:data-[slot=slider-range]:bg-amber-400 **:data-[slot=slider-thumb]:border-amber-400 **:data-[slot=slider-thumb]:ring-amber-400/50"
          value={localStars}
          min={MIN_STARS}
          max={MAX_STARS}
          step={1}
          onValueChange={setLocalStars}
          onValueCommit={handleStarsChange}
        />
      </Field>

      <Field>
        <FieldLabel>BPM</FieldLabel>
        <FieldDescription>
          Only show maps with BPM between <span className="text-primary font-semibold">{localBpm[0]}</span> and{" "}
          <span className="text-primary font-semibold">{localBpm[1]}</span>
        </FieldDescription>
        <Slider
          value={localBpm}
          min={MIN_BPM}
          max={MAX_BPM}
          step={1}
          onValueChange={setLocalBpm}
          onValueCommit={handleBpmChange}
        />
      </Field>

      <FieldSeparator />

      <Field>
        <FieldLabel className="flex items-center justify-between">Categories</FieldLabel>
        <FieldDescription>Only show maps from the following categories</FieldDescription>
        <div className="flex items-center gap-2">
          <Checkbox
            id="matchAnyCategory"
            checked={filters.matchAnyCategory}
            onCheckedChange={handleMatchAnyCategoryChange}
          />
          <Label htmlFor="matchAnyCategory" className="text-foreground">
            Match any
          </Label>
        </div>

        <div className="flex flex-wrap gap-2">
          {categories?.map((category) => {
            const isSelected = filters.categories.includes(category.id as number)

            return (
              <Button
                onClick={handleCategoriesSelect(category.id as number)}
                key={category.id}
                variant={isSelected ? "default" : "outline"}
                size="sm"
                className={cn("border", isSelected && "border-primary")}
              >
                {category.info?.name}
              </Button>
            )
          })}
        </div>
      </Field>

      <Button onClick={handleResetFilters} variant="destructive">
        Reset Filters
      </Button>
    </FieldGroup>
  )
}

export default GuildMapsFilters
