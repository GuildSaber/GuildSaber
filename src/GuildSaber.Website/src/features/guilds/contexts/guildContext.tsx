import type { GuildExtended } from "@/client"
import React, { createContext, useContext, type ReactNode } from "react"

const GuildContext = createContext<GuildExtended | undefined>(undefined)

interface GuildContextProviderProps {
  guild?: GuildExtended
  children: ReactNode
}

export const GuildContextProvider: React.FC<GuildContextProviderProps> = ({ guild, children }) => (
  <GuildContext.Provider value={guild}>{children}</GuildContext.Provider>
)

export const useGuildContext = (): GuildExtended | undefined => {
  const context = useContext(GuildContext)

  return context
}
