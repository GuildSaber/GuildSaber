import { Skeleton } from "@/components/ui/skeleton"

export const MapHeaderSkeleton = () => (
  <div className="flex flex-col gap-4">
    <div className="xs:flex-row xs:gap-6 flex flex-col items-center gap-4">
      <Skeleton className="xs:size-36 size-28 shrink-0 rounded-lg lg:size-44" />

      <div className="xs:items-start flex flex-1 flex-col items-center gap-2">
        <Skeleton className="h-5 w-2/3 md:h-8" />
        <Skeleton className="h-3 w-1/2 md:h-4" />

        <div className="mt-2 flex gap-1.5 md:gap-2">
          <Skeleton className="h-6 w-16 rounded md:h-7 md:w-24" />
          <Skeleton className="h-6 w-14 rounded md:h-7 md:w-20" />
          <Skeleton className="h-6 w-14 rounded md:h-7 md:w-20" />
        </div>

        <div className="xs:flex mt-1 hidden gap-2">
          <Skeleton className="h-7 w-20 rounded" />
          <Skeleton className="h-7 w-20 rounded" />
          <Skeleton className="h-7 w-20 rounded" />
          <Skeleton className="h-7 w-20 rounded" />
        </div>

        <div className="xs:flex mt-1 hidden gap-1">
          <Skeleton className="h-7 w-9 rounded" />
          <Skeleton className="h-7 w-9 rounded" />
          <Skeleton className="h-7 w-9 rounded" />
          <Skeleton className="h-7 w-9 rounded" />
        </div>
      </div>
    </div>

    <div className="xs:hidden flex justify-center gap-2">
      <Skeleton className="h-7 w-20 rounded" />
      <Skeleton className="h-7 w-20 rounded" />
      <Skeleton className="h-7 w-20 rounded" />
      <Skeleton className="h-7 w-20 rounded" />
    </div>

    <div className="xs:hidden flex gap-1">
      <Skeleton className="h-7 flex-1 rounded" />
      <Skeleton className="h-7 flex-1 rounded" />
      <Skeleton className="h-7 flex-1 rounded" />
      <Skeleton className="h-7 flex-1 rounded" />
    </div>
  </div>
)
