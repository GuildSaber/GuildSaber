import { cn } from "@/lib/utils"
import { useState, type ComponentProps } from "react"

interface Props extends ComponentProps<"img"> {
  banner?: boolean
}

const Image = ({ src, banner, className, onLoad, ...props }: Props) => {
  const fallback = (() => {
    if (banner) {
      return "/images/fallback-banner.svg"
    }

    return "/images/fallback.svg"
  })()

  const [erroredSrc, setErroredSrc] = useState<typeof src>()
  const [loadedSrc, setLoadedSrc] = useState<typeof src>()

  const imgSrc = !src || src === erroredSrc ? fallback : src
  const isLoaded = imgSrc === loadedSrc

  const handleError = () => setErroredSrc(src)

  const handleLoad: ComponentProps<"img">["onLoad"] = (e) => {
    setLoadedSrc(imgSrc)
    onLoad?.(e)
  }

  return (
    <div className="grid">
      <div
        aria-hidden
        className={cn(
          "bg-input pointer-events-none col-start-1 row-start-1 transition-opacity duration-100",
          isLoaded ? "opacity-0" : "animate-pulse opacity-100",
          className,
        )}
      />
      <img
        data-slot="image"
        src={imgSrc}
        onError={handleError}
        onLoad={handleLoad}
        className={cn(
          "col-start-1 row-start-1 transition-opacity duration-100",
          isLoaded ? "opacity-100" : "opacity-0",
          className,
        )}
        {...props}
      />
    </div>
  )
}

export default Image
