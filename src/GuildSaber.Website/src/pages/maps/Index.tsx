import { getRankedMapOptions } from "@/client/@tanstack/react-query.gen"
import ErrorState from "@/components/ErrorState"
import { Separator } from "@/components/ui/separator"
import DialogReplay from "@/features/maps/components/DialogReplay"
import MapHeader from "@/features/maps/components/MapHeader"
import MapLeaderboard from "@/features/maps/components/MapLeaderboard"
import { MapContextProvider } from "@/features/maps/contexts/mapContext"
import { useQuery } from "@tanstack/react-query"
import { SearchX } from "lucide-react"
import { useParams } from "react-router"

const MapsPage = () => {
  const { mapId } = useParams()

  const { data: map, isLoading } = useQuery({
    ...getRankedMapOptions({ path: { rankedMapId: mapId ?? "" } }),
    enabled: Boolean(mapId),
  })

  if (!map && !isLoading) {
    return (
      <ErrorState
        icon={SearchX}
        title="Map not found"
        variant="warning"
        description="The map you're looking for doesn't exist or has been removed."
      />
    )
  }

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
