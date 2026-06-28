import { Card, CardContent } from "@/components/ui/card"
import { Skeleton } from "@/components/ui/skeleton"

export const MapHeaderSkeleton = () => (
  <Card className="overflow-hidden p-0">
    <CardContent className="flex flex-col gap-4 p-4">
      <div className="flex items-center gap-4 md:gap-6">
        <Skeleton className="size-24 shrink-0 rounded-lg sm:size-44 lg:size-52" />

        <div className="flex flex-1 flex-col gap-2">
          <Skeleton className="h-5 w-2/3 sm:h-8" />
          <Skeleton className="h-3 w-1/3 sm:h-5" />
          <Skeleton className="h-3 w-1/2 sm:h-4" />

          <div className="mt-2 flex gap-1.5 sm:gap-2">
            <Skeleton className="h-6 w-16 rounded sm:h-7 sm:w-24" />
            <Skeleton className="h-6 w-14 rounded sm:h-7 sm:w-20" />
            <Skeleton className="h-6 w-14 rounded sm:h-7 sm:w-20" />
          </div>

          <div className="mt-1 hidden gap-2 sm:flex">
            <Skeleton className="h-7 w-20 rounded" />
            <Skeleton className="h-7 w-20 rounded" />
            <Skeleton className="h-7 w-20 rounded" />
            <Skeleton className="h-7 w-20 rounded" />
          </div>

          <div className="mt-1 hidden gap-1 sm:flex">
            <Skeleton className="h-7 w-9 rounded" />
            <Skeleton className="h-7 w-9 rounded" />
            <Skeleton className="h-7 w-9 rounded" />
            <Skeleton className="h-7 w-9 rounded" />
          </div>
        </div>
      </div>

      <div className="flex gap-2 sm:hidden">
        <Skeleton className="h-7 w-20 rounded" />
        <Skeleton className="h-7 w-20 rounded" />
        <Skeleton className="h-7 w-20 rounded" />
        <Skeleton className="h-7 w-20 rounded" />
      </div>

      <div className="flex gap-1 sm:hidden">
        <Skeleton className="h-7 flex-1 rounded" />
        <Skeleton className="h-7 flex-1 rounded" />
        <Skeleton className="h-7 flex-1 rounded" />
        <Skeleton className="h-7 flex-1 rounded" />
      </div>
    </CardContent>
  </Card>
)
