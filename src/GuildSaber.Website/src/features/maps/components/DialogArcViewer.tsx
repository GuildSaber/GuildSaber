import { AlertDialog, AlertDialogContent } from "@/components/ui/alert-dialog"
import { useArcViewerStore } from "@/features/maps/stores/arcViewerStore"
import { useRef, type RefObject } from "react"
import { useOnClickOutside } from "usehooks-ts"

const DialogArcViewer = () => {
  const { open, key, difficulty, gamemode, closeArcViewer } = useArcViewerStore()
  const ref = useRef<HTMLDivElement>(null)
  useOnClickOutside(ref as RefObject<HTMLDivElement>, closeArcViewer)

  const generateArcViewerUrl = () =>
    `https://allpoland.github.io/ArcViewer/?id=${key}&mode=${gamemode}&difficulty=${difficulty}`

  return (
    <AlertDialog open={open}>
      <AlertDialogContent ref={ref} className="aspect-video min-w-2/3 p-0">
        {key && <iframe width="100%" height="100%" src={generateArcViewerUrl()} />}
      </AlertDialogContent>
    </AlertDialog>
  )
}

export default DialogArcViewer
