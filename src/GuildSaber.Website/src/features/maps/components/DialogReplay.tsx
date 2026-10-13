import { AlertDialog, AlertDialogContent } from "@/components/ui/alert-dialog"
import type { Score } from "@/client"
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select"
import ReplayViewerIcon from "@/features/maps/components/ReplayViewerIcon"
import ReplayViewerPicker from "@/features/maps/components/ReplayViewerPicker"
import { REPLAY_VIEWERS, useReplayStore, type ReplayViewer } from "@/features/maps/stores/replayStore"
import { LEADERBOARD } from "@/utils/constants"
import { useRef } from "react"
import { useEventListener } from "usehooks-ts"

const getReplayUrl = (score: Score, viewer: ReplayViewer) => {
  if (score.type === LEADERBOARD.BeatLeader && score.beatLeaderScoreId) {
    return viewer === "ChroViewer"
      ? `https://chroviewer.com/?scoreIdBL=${score.beatLeaderScoreId}`
      : `https://replay.beatleader.com/?scoreId=${score.beatLeaderScoreId}`
  }

  if (score.type === LEADERBOARD.ScoreSaber && viewer === "ChroViewer") {
    return `https://chroviewer.com/?scoreId=${score.scoreSaberScoreId}`
  }

  return null
}

const DialogReplay = () => {
  const { open, score, viewer, setViewer, closeReplay } = useReplayStore()
  const ref = useRef<HTMLDivElement>(null)

  useEventListener(
    "pointerdown",
    (event) => {
      const isOutside = ref.current && !ref.current.contains(event.target as Node)
      const isSelectOpen = document.querySelector("[data-slot=select-content]")

      if (isOutside && !isSelectOpen) {
        closeReplay()
      }
    },
    undefined,
    { capture: true },
  )

  if (!viewer) {
    return (
      <AlertDialog open={open}>
        <AlertDialogContent ref={ref} className="overflow-hidden p-0 sm:max-w-2xl">
          <ReplayViewerPicker />
        </AlertDialogContent>
      </AlertDialog>
    )
  }

  const replayUrl = score ? getReplayUrl(score, viewer) : null

  return (
    <AlertDialog open={open}>
      <AlertDialogContent ref={ref} className="aspect-video min-w-2/3 p-0">
        {replayUrl ? (
          <iframe allow="fullscreen" width="100%" height="100%" src={replayUrl} className="rounded-lg" />
        ) : (
          <p className="text-muted-foreground flex h-full items-center justify-center text-sm">
            No replay available for this score.
          </p>
        )}

        <Select value={viewer} onValueChange={(value) => setViewer(value as ReplayViewer)}>
          <SelectTrigger size="sm" className="bg-background! absolute top-full left-0 mt-2">
            <SelectValue />
          </SelectTrigger>
          <SelectContent>
            {REPLAY_VIEWERS.map((option) => (
              <SelectItem key={option} value={option}>
                <ReplayViewerIcon viewer={option} />
                {option}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
      </AlertDialogContent>
    </AlertDialog>
  )
}

export default DialogReplay
