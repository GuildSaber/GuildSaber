import { AlertDialog, AlertDialogContent } from "@/components/ui/alert-dialog"
import { useReplayStore } from "@/features/maps/stores/replayStore"
import { LEADERBOARD } from "@/utils/constants"
import { useRef, type RefObject } from "react"
import { useOnClickOutside } from "usehooks-ts"

const getReplayUrl = (score: NonNullable<ReturnType<typeof useReplayStore.getState>["score"]>) => {
  if (score.type === LEADERBOARD.BeatLeader && score.beatLeaderScoreId) {
    return `https://replay.beatleader.com/?scoreId=${score.beatLeaderScoreId}`
  }

  return null
}

const DialogReplay = () => {
  const { open, score, closeReplay } = useReplayStore()
  const ref = useRef<HTMLDivElement>(null)
  useOnClickOutside(ref as RefObject<HTMLDivElement>, closeReplay)

  const replayUrl = score ? getReplayUrl(score) : null

  return (
    <AlertDialog open={open}>
      <AlertDialogContent ref={ref} className="aspect-video min-w-2/3 overflow-hidden p-0">
        {replayUrl ? (
          <iframe allow="fullscreen" width="100%" height="100%" src={replayUrl} />
        ) : (
          <p className="text-muted-foreground flex h-full items-center justify-center text-sm">
            No replay available for this score.
          </p>
        )}
      </AlertDialogContent>
    </AlertDialog>
  )
}

export default DialogReplay
