import { create } from "zustand"
import { createJSONStorage, persist } from "zustand/middleware"

type GuildsStore = {
  selectedGuild: number | null
  setSelectedGuild: (_guildId: number | null) => void
}

export const useGuildsStore = create<GuildsStore>()(
  persist(
    (set) => ({
      selectedGuild: null,
      setSelectedGuild: (guildId) => set({ selectedGuild: guildId }),
    }),
    {
      name: "guilds",
      storage: createJSONStorage(() => localStorage),
    },
  ),
)
