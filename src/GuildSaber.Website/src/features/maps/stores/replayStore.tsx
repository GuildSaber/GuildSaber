import type { Score } from "@/client"
import { create } from "zustand"

interface ReplayStore {
  open: boolean
  score: Score | null
  openReplay: (score: Score) => void
  closeReplay: () => void
}

export const useReplayStore = create<ReplayStore>((set) => ({
  open: false,
  score: null,
  openReplay: (score) => set({ open: true, score }),
  closeReplay: () => set({ open: false, score: null }),
}))
