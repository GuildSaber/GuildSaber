import { Card, CardContent } from "@/components/ui/card"
import { Skeleton } from "@/components/ui/skeleton"

const MapLeaderboardRowSkeleton = () => (
  <Card className="py-3">
    <CardContent className="grid grid-cols-[auto_minmax(0,1fr)] items-center gap-x-3 gap-y-2 px-3 md:grid-cols-[auto_minmax(0,1fr)_auto_auto_auto_auto]">
      <Skeleton className="mx-auto h-4 w-6" />

      <div className="flex min-w-0 items-center gap-2 md:contents">
        <div className="flex min-w-0 flex-1 items-center gap-2">
          <Skeleton className="size-6 shrink-0 rounded-lg md:size-8" />
          <Skeleton className="h-3.5 w-5 shrink-0" />
          <Skeleton className="h-4 max-w-28 flex-1" />
        </div>
        <Skeleton className="h-4 w-16 shrink-0" />
        <Skeleton className="h-6 w-8 rounded" />
      </div>

      <div className="col-span-full mt-1 grid grid-cols-2 gap-2 md:col-span-2 md:mt-0">
        <Skeleton className="h-5 md:w-20" />
        <Skeleton className="h-5 md:w-16" />
        <Skeleton className="h-5 md:w-16" />
        <Skeleton className="h-5 md:w-16" />
      </div>
    </CardContent>
  </Card>
)

export const MapLeaderboardSkeleton = () => (
  <div className="flex flex-col gap-3">
    {Array.from({ length: 10 }).map((_, i) => (
      <MapLeaderboardRowSkeleton key={i} />
    ))}
  </div>
)
