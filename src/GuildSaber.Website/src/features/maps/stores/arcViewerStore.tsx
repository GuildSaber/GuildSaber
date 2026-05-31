import { create } from "zustand"

interface ArcViewerStore {
  open: boolean
  key: string | null
  difficulty: string | null
  gamemode: string | null
  openArcViewer: (_key: string | null, _difficulty: string, _gamemode: string) => void
  closeArcViewer: () => void
}

export const useArcViewerStore = create<ArcViewerStore>((set) => ({
  open: false,
  key: null,
  difficulty: null,
  gamemode: null,
  openArcViewer: (key, difficulty, gamemode) => set({ open: true, key, difficulty, gamemode }),
  closeArcViewer: () => set({ open: false, key: null, difficulty: null, gamemode: null }),
}))
