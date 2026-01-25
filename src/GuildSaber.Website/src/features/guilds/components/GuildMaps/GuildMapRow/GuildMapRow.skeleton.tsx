import { Skeleton } from "@/components/ui/skeleton"

export const GuildMapRowSkeleton = () => (
  <div className="grid grid-cols-[auto_1fr_auto] items-center gap-4 py-2 sm:grid-cols-[auto_1fr_auto_auto]">
    <div className="relative size-16 md:size-18">
      <Skeleton className="size-16 rounded md:size-18" />
    </div>

    <div className="space-y-2">
      <Skeleton className="h-5 w-48 max-w-full" />
      <Skeleton className="h-4 w-32" />
    </div>

    <div className="flex flex-col items-end justify-end gap-1">
      <Skeleton className="h-7 w-16 rounded" />
      <Skeleton className="h-7 w-16 rounded" />
    </div>

    <div className="grid grid-cols-4 items-end gap-1 sm:grid-cols-2">
      <Skeleton className="size-8 rounded" />
      <Skeleton className="size-8 rounded" />
      <Skeleton className="size-8 rounded" />
      <Skeleton className="size-8 rounded" />
    </div>
  </div>
)
