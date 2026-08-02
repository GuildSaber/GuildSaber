import Image from "@/components/Image"
import { MapHeaderActions } from "@/features/maps/components/MapHeader/MapHeaderActions"
import { MapHeaderInfo } from "@/features/maps/components/MapHeader/MapHeaderInfo"
import { MapHeaderRequirements } from "@/features/maps/components/MapHeader/MapHeaderRequirements"
import { MapHeaderSkeleton } from "@/features/maps/components/MapHeader/MapHeaderSkeleton"
import { MapStats } from "@/features/maps/components/MapHeader/MapHeaderStats"
import { useMapContext } from "@/features/maps/contexts/mapContext"
import { getMapCover } from "@/utils/beatsaver"
import { formatTime } from "@/utils/time"

const MapHeader = () => {
  const map = useMapContext()

  if (!map) {
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
    <div className="flex flex-col gap-4">
      <div className="xs:flex-row xs:gap-6 flex flex-col items-center gap-4">
        <div className="shrink-0">
          <Image src={cover} className="xs:size-36 size-28 rounded-lg md:size-44" />
        </div>

        <div className="xs:items-start flex flex-1 flex-col items-center gap-1">
          <MapHeaderInfo map={map} />
          <MapStats stats={stats} className="xs:flex mt-1 hidden" />
          <MapHeaderActions map={map} className="xs:flex mt-1 hidden" />
        </div>
      </div>

      <MapStats stats={stats} className="xs:hidden justify-center" />
      <MapHeaderActions map={map} className="xs:hidden w-full *:flex-1" />
      <MapHeaderRequirements map={map} />
    </div>
  )
}

export default MapHeader
