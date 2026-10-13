import ReplayViewerIcon from "@/features/maps/components/ReplayViewerIcon"
import { REPLAY_VIEWERS, useReplayStore, type ReplayViewer } from "@/features/maps/stores/replayStore"

const VIEWER_DESCRIPTIONS: Record<ReplayViewer, string> = {
  BeatLeader: "The official BeatLeader viewer. More technical, with detailed stats and analysis tools.",
  ChroViewer: "Nicer to watch, with environments and lighting similar to the real game.",
}

const ReplayViewerPicker = () => {
  const { setViewer } = useReplayStore()

  return (
    <div className="flex h-full flex-col items-center justify-center gap-6 p-6">
      <div className="text-center">
        <h2 className="text-lg font-semibold">Choose a replay viewer</h2>
        <p className="text-muted-foreground text-sm">You can change it later from the replay window.</p>
      </div>

      <div className="grid w-full max-w-2xl gap-3 sm:grid-cols-2">
        {REPLAY_VIEWERS.map((viewer) => (
          <button
            key={viewer}
            type="button"
            onClick={() => setViewer(viewer)}
            className="hover:border-primary hover:bg-accent/50 flex cursor-pointer flex-col items-center gap-3 rounded-xl border p-6 text-center transition-colors"
          >
            <ReplayViewerIcon viewer={viewer} className="size-14" />
            <span className="font-semibold">{viewer}</span>
            <span className="text-muted-foreground text-sm">{VIEWER_DESCRIPTIONS[viewer]}</span>
          </button>
        ))}
      </div>
    </div>
  )
}

export default ReplayViewerPicker
