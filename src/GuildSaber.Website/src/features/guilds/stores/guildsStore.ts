import { create } from "zustand"
import { createJSONStorage, persist } from "zustand/middleware"

interface GuildsStore {
  selectedGuild: number | null
  setSelectedGuild: (_guildId: number | null) => void
}

export const useGuildsStore = create<GuildsStore>()(
  persist(
    (set, get) => ({
      selectedGuild: null,
      setSelectedGuild: (guildId) => {
        if (guildId === get().selectedGuild) {
          return
        }

        set({ selectedGuild: guildId })
      },
    }),
    {
      name: "guilds",
      storage: createJSONStorage(() => localStorage),
    },
  ),
)
