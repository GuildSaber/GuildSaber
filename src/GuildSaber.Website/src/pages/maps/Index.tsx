import { getRankedMapOptions } from "@/client/@tanstack/react-query.gen"
import { Separator } from "@/components/ui/separator"
import DialogReplay from "@/features/maps/components/DialogReplay"
import MapHeader from "@/features/maps/components/MapHeader"
import MapLeaderboard from "@/features/maps/components/MapLeaderboard"
import { MapContextProvider } from "@/features/maps/contexts/mapContext"
import { useQuery } from "@tanstack/react-query"
import { useParams } from "react-router"

const MapsPage = () => {
  const { mapId } = useParams()

  const { data: map } = useQuery({
    ...getRankedMapOptions({ path: { rankedMapId: mapId ?? "" } }),
    enabled: Boolean(mapId),
  })

  return (
    <MapContextProvider map={map}>
      <main className="space-y-3">
        <MapHeader />
        <Separator className="my-6 h-0.5!" />
        <MapLeaderboard />
        <DialogReplay />
      </main>
    </MapContextProvider>
  )
}

export default MapsPage
