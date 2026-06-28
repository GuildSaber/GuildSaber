import { Skeleton } from "@/components/ui/skeleton"

const MapLeaderboardRowSkeleton = () => (
  <div className="grid grid-cols-[2rem_2rem_minmax(0,1fr)_auto] items-center gap-3 py-2.5 sm:grid-cols-[2rem_2rem_minmax(0,1fr)_auto_auto_auto]">
    <Skeleton className="ml-auto h-4 w-6 rounded" />
    <Skeleton className="size-8 rounded-full" />
    <Skeleton className="h-4 w-32 rounded" />
    <Skeleton className="hidden h-4 w-20 rounded sm:block" />
    <Skeleton className="h-4 w-14 rounded" />
  </div>
)

export const MapLeaderboardSkeleton = () => (
  <div className="divide-y">
    {Array.from({ length: 10 }).map((_, i) => (
      <MapLeaderboardRowSkeleton key={i} />
    ))}
  </div>
)
