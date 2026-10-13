import type { Score } from "@/client"
import { create } from "zustand"
import { createJSONStorage, persist } from "zustand/middleware"

export const REPLAY_VIEWERS = ["BeatLeader", "ChroViewer"] as const

export type ReplayViewer = (typeof REPLAY_VIEWERS)[number]

interface ReplayStore {
  open: boolean
  score: Score | null
  viewer: ReplayViewer | null
  openReplay: (score: Score) => void
  setViewer: (viewer: ReplayViewer) => void
  closeReplay: () => void
}

export const useReplayStore = create<ReplayStore>()(
  persist(
    (set) => ({
      open: false,
      score: null,
      viewer: null,
      openReplay: (score) => set({ open: true, score }),
      setViewer: (viewer) => set({ viewer }),
      closeReplay: () => set({ open: false, score: null }),
    }),
    {
      name: "replay",
      storage: createJSONStorage(() => localStorage),
      partialize: ({ viewer }) => ({ viewer }),
    },
  ),
)
