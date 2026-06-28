import { getRankedMapOptions } from "@/client/@tanstack/react-query.gen"
import Image from "@/components/Image"
import { Card, CardContent } from "@/components/ui/card"
import { getMapCover } from "@/utils/beatsaver"
import { formatTime } from "@/utils/time"
import { useQuery } from "@tanstack/react-query"
import { MapHeaderActions } from "./MapHeaderActions"
import { MapHeaderInfo } from "./MapHeaderInfo"
import { MapHeaderRequirements } from "./MapHeaderRequirements"
import { MapHeaderSkeleton } from "./MapHeaderSkeleton"
import { MapStats } from "./MapHeaderStats"

interface Props {
  mapId: string | undefined
}

const MapHeader = ({ mapId }: Props) => {
  const { data: map, isLoading } = useQuery({
    ...getRankedMapOptions({ path: { rankedMapId: mapId || "" } }),
    enabled: Boolean(mapId),
  })

  if (isLoading || !map) {
    return <MapHeaderSkeleton />
  }

  const [{ song, difficulty: diffKey }] = map.versions
  const cover = getMapCover(song.hash)
  const stats = {
    bpm: song.stats.bpm,
    njs: diffKey.stats.njs,
    nps: diffKey.stats.notesPerSecond,
    duration: formatTime(Number(song.stats.durationSec)),
  }

  return (
    <Card className="overflow-hidden p-0">
      <CardContent className="flex flex-col gap-4 p-4">
        <div className="flex items-center gap-4 sm:gap-6">
          <div className="shrink-0">
            <Image src={cover} className="size-24 rounded-lg sm:size-44 lg:size-52" />
          </div>

          <div className="flex flex-1 flex-col gap-1">
            <MapHeaderInfo map={map} />
            <MapStats stats={stats} className="mt-1 hidden sm:flex" />
            <MapHeaderActions map={map} className="mt-1 hidden sm:flex" />
          </div>
        </div>

        <MapStats stats={stats} className="sm:hidden" />
        <MapHeaderActions map={map} className="w-full *:flex-1 sm:hidden" />
        <MapHeaderRequirements map={map} />
      </CardContent>
    </Card>
  )
}

export default MapHeader
