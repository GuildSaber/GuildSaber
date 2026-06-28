import type { RankedMap } from "@/client"
import { createContext, type FC, useContext, type ReactNode } from "react"

const MapContext = createContext<RankedMap | undefined>(undefined)

interface MapContextProviderProps {
  map?: RankedMap
  children: ReactNode
}

export const MapContextProvider: FC<MapContextProviderProps> = ({ map, children }) => (
  <MapContext.Provider value={map}>{children}</MapContext.Provider>
)

export const useMapContext = (): RankedMap | undefined => useContext(MapContext)
