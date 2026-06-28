import MapHeader from "@/features/maps/components/MapHeader"
import { useParams } from "react-router"

const MapsPage = () => {
  const { mapId } = useParams()

  return (
    <main className="space-y-3">
      <MapHeader mapId={mapId} />
    </main>
  )
}

export default MapsPage
