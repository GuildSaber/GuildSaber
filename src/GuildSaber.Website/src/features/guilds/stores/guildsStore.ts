import { create } from "zustand"
import { createJSONStorage, persist } from "zustand/middleware"

interface GuildsStore {
  selectedGuild: number | null
  setSelectedGuild: (_guildId: number | null) => void
  clear: () => void
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
      clear: () => {
        set({ selectedGuild: null })
      },
    }),
    {
      name: "guilds",
      storage: createJSONStorage(() => localStorage),
    },
  ),
)
